package com.dinder.app.domain.repository

import com.dinder.app.domain.model.Notification

/**
 * Repository for notification operations.
 */
interface NotificationRepository {
    suspend fun getNotifications(cursor: String? = null): Result<Pair<List<Notification>, String?>>
    suspend fun registerDeviceToken(token: String): Result<Unit>
    suspend fun markRead(notificationIds: List<String>? = null): Result<Int>
    suspend fun updateOptOut(type: String, optOut: Boolean): Result<Unit>

    // SignalR lifecycle
    suspend fun connectNotificationHub()
    suspend fun disconnectNotificationHub()
}
