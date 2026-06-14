package com.dinder.app.data.repository

import com.dinder.app.data.remote.ApiService
import com.dinder.app.data.remote.SignalRClient
import com.dinder.app.data.remote.SignalRMessage
import com.dinder.app.di.TokenRefreshedException
import com.dinder.app.domain.model.Conversation
import com.dinder.app.domain.model.Message
import com.dinder.app.domain.repository.ChatRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.launch
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.decodeFromJsonElement

/**
 * Repository for chat operations: REST endpoints + SignalR ChatHub integration.
 * Maps DTOs to domain models and exposes real-time message/typing/read flows.
 */
class ChatRepositoryImpl(
    private val apiService: ApiService,
    private val chatSignalRClient: SignalRClient,
    private val appScope: CoroutineScope
) : ChatRepository {

    private val json = Json { ignoreUnknownKeys = true }

    // ── Real-time event flows ──────────────────────────────────────────

    private val _newMessages = MutableSharedFlow<Message>(replay = 0, extraBufferCapacity = 64)
    val newMessages: SharedFlow<Message> = _newMessages.asSharedFlow()

    private val _typingUpdates = MutableSharedFlow<SignalRMessage.TypingUpdatePayload>(replay = 0, extraBufferCapacity = 16)
    val typingUpdates: SharedFlow<SignalRMessage.TypingUpdatePayload> = _typingUpdates.asSharedFlow()

    private val _messageRead = MutableSharedFlow<SignalRMessage.MessageReadPayload>(replay = 0, extraBufferCapacity = 16)
    val messageRead: SharedFlow<SignalRMessage.MessageReadPayload> = _messageRead.asSharedFlow()

    init {
        // Collect all SignalR messages and dispatch to typed flows
        appScope.launch {
            chatSignalRClient.messages.collect { envelope ->
                try {
                    when (envelope.target) {
                        "ReceiveMessage" -> {
                            envelope.arguments?.firstOrNull()?.let { arg ->
                                val payload = json.decodeFromJsonElement(
                                    SignalRMessage.ReceiveMessagePayload.serializer(), arg
                                )
                                _newMessages.emit(
                                    Message(
                                        messageId = payload.messageId,
                                        conversationId = payload.conversationId,
                                        senderId = payload.senderId,
                                        content = payload.content,
                                        sentAt = payload.sentAt
                                    )
                                )
                            }
                        }
                        "TypingUpdate" -> {
                            envelope.arguments?.firstOrNull()?.let { arg ->
                                val payload = json.decodeFromJsonElement(
                                    SignalRMessage.TypingUpdatePayload.serializer(), arg
                                )
                                _typingUpdates.emit(payload)
                            }
                        }
                        "MessageRead" -> {
                            envelope.arguments?.firstOrNull()?.let { arg ->
                                val payload = json.decodeFromJsonElement(
                                    SignalRMessage.MessageReadPayload.serializer(), arg
                                )
                                _messageRead.emit(payload)
                            }
                        }
                    }
                } catch (_: Exception) {
                    // Skip unparseable envelopes silently
                }
            }
        }
    }

    // ── Token retry helper ─────────────────────────────────────────────

    private suspend fun <T> withTokenRetry(block: suspend () -> T): Result<T> =
        try {
            Result.success(block())
        } catch (e: TokenRefreshedException) {
            try {
                Result.success(block())
            } catch (e2: Exception) {
                Result.failure(e2)
            }
        } catch (e: Exception) {
            Result.failure(e)
        }

    // ── REST operations ────────────────────────────────────────────────

    override suspend fun getConversations(cursor: String?): Result<Pair<List<Conversation>, String?>> =
        withTokenRetry {
            val res = apiService.getConversations(cursor)
            val conversations = res.conversations.map { dto ->
                Conversation(
                    conversationId = dto.conversationId,
                    displayName = dto.displayName,
                    lastMessage = dto.lastMessage,
                    unreadCount = dto.unreadCount,
                    icebreakerQuestion = dto.icebreakerQuestion,
                    icebreakerCategory = dto.icebreakerCategory
                )
            }
            conversations to res.nextCursor
        }

    override suspend fun getMessages(
        conversationId: String, cursor: String?
    ): Result<Pair<List<Message>, String?>> = withTokenRetry {
        val res = apiService.getMessages(conversationId, cursor)
        val messages = res.messages.map { dto ->
            Message(
                messageId = dto.messageId,
                conversationId = conversationId,
                senderId = dto.senderId,
                content = dto.content,
                sentAt = dto.sentAt,
                readAt = dto.readAt
            )
        }
        messages to res.nextCursor
    }

    override suspend fun unmatch(conversationId: String): Result<Unit> = withTokenRetry {
        apiService.unmatch(conversationId)
        Unit
    }

    // ── SignalR operations ─────────────────────────────────────────────

    /** Send message via SignalR ChatHub. Returns a pending message that will be confirmed via ReceiveMessage. */
    override suspend fun sendMessage(conversationId: String, content: String): Result<Message> = runCatching {
        chatSignalRClient.invoke(
            "SendMessage",
            JsonPrimitive(conversationId),
            JsonPrimitive(content)
        )
        // Return optimistic pending message — server will push confirmed ReceiveMessage
        Message(
            messageId = "pending-${System.currentTimeMillis()}",
            conversationId = conversationId,
            senderId = "self", // will be replaced by confirmed message
            content = content,
            sentAt = java.time.Instant.now().toString()
        )
    }

    /** Send typing indicator (true=typing, false=stopped). Debounce is handled by ViewModel. */
    suspend fun sendTypingIndicator(conversationId: String, isTyping: Boolean) {
        runCatching {
            chatSignalRClient.invoke(
                "TypingIndicator",
                JsonPrimitive(conversationId),
                JsonPrimitive(isTyping)
            )
        }
    }

    /** Notify server that messages in this conversation have been read. */
    suspend fun sendMarkRead(conversationId: String) {
        runCatching {
            chatSignalRClient.invoke("MarkRead", JsonPrimitive(conversationId))
        }
    }

    // ── Connection lifecycle ───────────────────────────────────────────

    /** Connect to ChatHub (non-blocking). Launches connect in app scope. */
    override suspend fun connectChatHub() {
        appScope.launch {
            try {
                chatSignalRClient.connect()
            } catch (_: Exception) {
                // Reconnect is handled by SignalRClient internally
            }
        }
    }

    override suspend fun joinConversation(conversationId: String) {
        chatSignalRClient.invoke("JoinConversation", JsonPrimitive(conversationId))
    }

    override suspend fun leaveConversation(conversationId: String) {
        runCatching {
            chatSignalRClient.invoke("LeaveConversation", JsonPrimitive(conversationId))
        }
    }

    override suspend fun disconnectChatHub() {
        chatSignalRClient.disconnect()
    }
}
