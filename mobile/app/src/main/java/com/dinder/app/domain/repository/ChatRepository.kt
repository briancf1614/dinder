package com.dinder.app.domain.repository

import com.dinder.app.domain.model.Conversation
import com.dinder.app.domain.model.Message

/**
 * Repository for chat operations (REST + SignalR).
 */
interface ChatRepository {
    suspend fun getConversations(cursor: String? = null): Result<Pair<List<Conversation>, String?>>
    suspend fun getMessages(conversationId: String, cursor: String? = null): Result<Pair<List<Message>, String?>>
    suspend fun sendMessage(conversationId: String, content: String): Result<Message>
    suspend fun unmatch(conversationId: String): Result<Unit>

    // SignalR lifecycle
    suspend fun connectChatHub()
    suspend fun joinConversation(conversationId: String)
    suspend fun leaveConversation(conversationId: String)
    suspend fun disconnectChatHub()
}
