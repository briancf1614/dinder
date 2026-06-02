package com.dinder.app.presentation.screens.chat

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.dinder.app.data.repository.ChatRepositoryImpl
import com.dinder.app.domain.model.Conversation
import com.dinder.app.domain.model.Message
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class ChatViewModel @Inject constructor(
    private val chatRepository: ChatRepositoryImpl
) : ViewModel() {

    // ── Conversation List State ──────────────────────────────────────

    data class ConversationsUiState(
        val conversations: List<Conversation> = emptyList(),
        val isLoading: Boolean = false,
        val error: String? = null,
        val nextCursor: String? = null,
        val isLoadingMore: Boolean = false
    )

    private val _conversationsState = MutableStateFlow(ConversationsUiState())
    val conversationsState: StateFlow<ConversationsUiState> = _conversationsState.asStateFlow()

    // ── Active Chat State ────────────────────────────────────────────

    data class ChatUiState(
        val messages: List<Message> = emptyList(),
        val isLoading: Boolean = false,
        val isLoadingMore: Boolean = false,
        val error: String? = null,
        val nextCursor: String? = null,
        val currentUserId: String = "",
        val matchName: String = "",
        val icebreakerQuestion: String? = null,
        val typingDisplayName: String? = null,
        val inputText: String = "",
        val isSending: Boolean = false
    )

    private val _chatState = MutableStateFlow(ChatUiState())
    val chatState: StateFlow<ChatUiState> = _chatState.asStateFlow()

    // ── Navigation events ────────────────────────────────────────────

    private val _navigateToChat = MutableSharedFlow<String>()
    val navigateToChat: SharedFlow<String> = _navigateToChat.asSharedFlow()

    private val _navigateBack = MutableSharedFlow<Unit>()
    val navigateBack: SharedFlow<Unit> = _navigateBack.asSharedFlow()

    // ── Internal ─────────────────────────────────────────────────────

    private var activeConversationId: String? = null
    private var currentUserId: String = ""
    private var typingJob: Job? = null
    private var signalRCollectionJob: Job? = null

    // ── Typing debounce ──────────────────────────────────────────────

    private val _typingState = MutableStateFlow<Map<String, String>>(emptyMap()) // convId -> name
    val typingState: StateFlow<Map<String, String>> = _typingState.asStateFlow()

    init {
        // Collect real-time typing updates across all conversations
        viewModelScope.launch {
            chatRepository.typingUpdates.collect { update ->
                val current = _typingState.value.toMutableMap()
                if (update.isTyping) {
                    current[update.conversationId] = update.userId
                } else {
                    current.remove(update.conversationId)
                }
                _typingState.value = current

                // Update active chat typing display
                if (update.conversationId == activeConversationId) {
                    _chatState.value = _chatState.value.copy(
                        typingDisplayName = if (update.isTyping) "Someone" else null
                    )
                }
            }
        }
    }

    // ── Conversation List ────────────────────────────────────────────

    fun loadConversations() {
        viewModelScope.launch {
            _conversationsState.value = _conversationsState.value.copy(isLoading = true, error = null)
            chatRepository.getConversations()
                .onSuccess { (conversations, cursor) ->
                    _conversationsState.value = _conversationsState.value.copy(
                        conversations = conversations, isLoading = false, nextCursor = cursor
                    )
                }
                .onFailure { e ->
                    _conversationsState.value = _conversationsState.value.copy(
                        isLoading = false, error = e.message
                    )
                }
        }
    }

    fun loadMoreConversations() {
        val cursor = _conversationsState.value.nextCursor ?: return
        if (_conversationsState.value.isLoadingMore) return
        viewModelScope.launch {
            _conversationsState.value = _conversationsState.value.copy(isLoadingMore = true)
            chatRepository.getConversations(cursor)
                .onSuccess { (more, nextCursor) ->
                    _conversationsState.value = _conversationsState.value.copy(
                        conversations = _conversationsState.value.conversations + more,
                        isLoadingMore = false,
                        nextCursor = nextCursor
                    )
                }
                .onFailure {
                    _conversationsState.value = _conversationsState.value.copy(isLoadingMore = false)
                }
        }
    }

    // ── Chat ─────────────────────────────────────────────────────────

    fun openChat(conversation: Conversation) {
        activeConversationId = conversation.conversationId
        currentUserId = "self"
        _chatState.value = ChatUiState(
            matchName = conversation.displayName,
            icebreakerQuestion = conversation.icebreakerQuestion
        )
        loadMessages(conversation.conversationId)

        viewModelScope.launch {
            chatRepository.joinConversation(conversation.conversationId)
            chatRepository.sendMarkRead(conversation.conversationId)
            _navigateToChat.emit(conversation.conversationId)
        }

        startSignalRCollection()
    }

    fun loadMessages(conversationId: String, cursor: String? = null) {
        viewModelScope.launch {
            val isLoadMore = cursor != null
            _chatState.value = _chatState.value.copy(
                isLoading = !isLoadMore, isLoadingMore = isLoadMore, error = null
            )
            chatRepository.getMessages(conversationId, cursor)
                .onSuccess { (messages, nextCursor) ->
                    _chatState.value = _chatState.value.copy(
                        messages = if (isLoadMore) messages + _chatState.value.messages
                                   else messages,
                        isLoading = false, isLoadingMore = false, nextCursor = nextCursor
                    )
                }
                .onFailure { e ->
                    _chatState.value = _chatState.value.copy(
                        isLoading = false, isLoadingMore = false, error = e.message
                    )
                }
        }
    }

    fun onInputTextChanged(text: String) {
        _chatState.value = _chatState.value.copy(inputText = text)

        // Debounced typing indicator
        typingJob?.cancel()
        typingJob = viewModelScope.launch {
            delay(3000)
            activeConversationId?.let { chatRepository.sendTypingIndicator(it, false) }
        }
        if (text.isNotEmpty()) {
            viewModelScope.launch {
                activeConversationId?.let { chatRepository.sendTypingIndicator(it, true) }
            }
        }
    }

    fun sendMessage() {
        val text = _chatState.value.inputText.trim()
        val convId = activeConversationId ?: return
        if (text.isEmpty()) return

        viewModelScope.launch {
            _chatState.value = _chatState.value.copy(isSending = true)
            chatRepository.sendMessage(convId, text)
                .onSuccess { pendingMsg ->
                    _chatState.value = _chatState.value.copy(
                        messages = _chatState.value.messages + pendingMsg,
                        inputText = "",
                        isSending = false
                    )
                }
                .onFailure {
                    _chatState.value = _chatState.value.copy(
                        isSending = false, error = "Failed to send message"
                    )
                }
        }
    }

    fun unmatch() {
        val convId = activeConversationId ?: return
        viewModelScope.launch {
            chatRepository.unmatch(convId)
                .onSuccess {
                    leaveChatRoom()
                    _navigateBack.emit(Unit)
                    loadConversations() // Refresh list
                }
        }
    }

    fun leaveChatRoom() {
        activeConversationId?.let {
            viewModelScope.launch { chatRepository.leaveConversation(it) }
        }
        activeConversationId = null
        signalRCollectionJob?.cancel()
    }

    // ── Connection ───────────────────────────────────────────────────

    fun connectChatHub() {
        viewModelScope.launch { chatRepository.connectChatHub() }
    }

    fun disconnectChatHub() {
        viewModelScope.launch { chatRepository.disconnectChatHub() }
    }

    // ── Internal ─────────────────────────────────────────────────────

    private fun startSignalRCollection() {
        signalRCollectionJob?.cancel()
        signalRCollectionJob = viewModelScope.launch {
            chatRepository.newMessages.collect { msg ->
                if (msg.conversationId == activeConversationId) {
                    // Remove pending message with matching content/timestamp
                    val cleaned = _chatState.value.messages.filter {
                        !(it.messageId.startsWith("pending-") && it.content == msg.content)
                    }
                    _chatState.value = _chatState.value.copy(messages = cleaned + msg)
                }
                // Refresh conversations to update last message
                loadConversations()
            }
        }
    }
}
