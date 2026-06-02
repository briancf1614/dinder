package com.dinder.app.domain.model

/**
 * Domain model representing a chat conversation.
 */
data class Conversation(
    val conversationId: String,
    val displayName: String,
    val lastMessage: String? = null,
    val unreadCount: Int = 0,
    val icebreakerQuestion: String? = null,
    val icebreakerCategory: String? = null
)
