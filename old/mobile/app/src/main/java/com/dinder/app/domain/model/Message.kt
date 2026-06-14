package com.dinder.app.domain.model

/**
 * Domain model representing a chat message.
 */
data class Message(
    val messageId: String,
    val conversationId: String,
    val senderId: String,
    val content: String,
    val sentAt: String,
    val readAt: String? = null
)
