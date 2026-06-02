package com.dinder.app.domain.model

/**
 * Domain model representing an in-app notification.
 */
data class Notification(
    val notificationId: String,
    val type: String, // "Match" | "Message" | "Promotion"
    val title: String,
    val body: String? = null,
    val deepLinkPayload: String? = null,
    val isRead: Boolean = false,
    val createdAt: String
)
