package com.dinder.app.data.remote.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

// ── Response DTOs ─────────────────────────────────────────────────────

@Serializable
data class ConversationsResponse(
    val conversations: List<ConversationDto>,
    @SerialName("nextCursor")
    val nextCursor: String? = null
)

@Serializable
data class ConversationDto(
    @SerialName("conversationId")
    val conversationId: String,
    @SerialName("displayName")
    val displayName: String,
    @SerialName("lastMessage")
    val lastMessage: String? = null,
    @SerialName("unreadCount")
    val unreadCount: Int,
    @SerialName("icebreakerQuestion")
    val icebreakerQuestion: String? = null,
    @SerialName("icebreakerCategory")
    val icebreakerCategory: String? = null
)

@Serializable
data class MessagesResponse(
    val messages: List<MessageDto>,
    @SerialName("nextCursor")
    val nextCursor: String? = null
)

@Serializable
data class MessageDto(
    @SerialName("messageId")
    val messageId: String,
    @SerialName("senderId")
    val senderId: String,
    val content: String,
    @SerialName("sentAt")
    val sentAt: String,
    @SerialName("readAt")
    val readAt: String? = null
)
