package com.dinder.app.data.remote.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

// ── Request DTOs ──────────────────────────────────────────────────────

@Serializable
data class RegisterDeviceTokenRequest(
    val token: String,
    val platform: String // "Android"
)

@Serializable
data class MarkReadRequest(
    @SerialName("notificationIds")
    val notificationIds: List<String>? = null // null = mark all
)

@Serializable
data class UpdateOptOutRequest(
    val type: String,  // "Match" | "Message" | "Promotion"
    @SerialName("optOut")
    val optOut: Boolean
)

// ── Response DTOs ─────────────────────────────────────────────────────

@Serializable
data class NotificationsResponse(
    val notifications: List<NotificationDto>,
    @SerialName("nextCursor")
    val nextCursor: String? = null
)

@Serializable
data class NotificationDto(
    @SerialName("notificationId")
    val notificationId: String,
    val type: String, // "Match" | "Message" | "Promotion"
    val title: String,
    val body: String? = null,
    @SerialName("deepLinkPayload")
    val deepLinkPayload: String? = null,
    @SerialName("isRead")
    val isRead: Boolean,
    @SerialName("createdAt")
    val createdAt: String
)

@Serializable
data class MarkReadResponse(
    @SerialName("markedCount")
    val markedCount: Int
)
