package com.dinder.app.data.repository

import com.dinder.app.data.remote.ApiService
import com.dinder.app.data.remote.SignalRClient
import com.dinder.app.data.remote.SignalRMessage
import com.dinder.app.data.remote.dto.RegisterDeviceTokenRequest
import com.dinder.app.data.remote.dto.UpdateOptOutRequest
import com.dinder.app.di.TokenRefreshedException
import com.dinder.app.domain.model.Notification
import com.dinder.app.domain.repository.NotificationRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.decodeFromJsonElement

/**
 * Repository for notification operations: REST endpoints + SignalR NotificationHub integration.
 * Maps DTOs to domain models and exposes real-time notification + badge flows.
 */
class NotificationRepositoryImpl(
    private val apiService: ApiService,
    private val notificationSignalRClient: SignalRClient,
    private val appScope: CoroutineScope
) : NotificationRepository {

    private val json = Json { ignoreUnknownKeys = true }

    // ── Real-time event flows ──────────────────────────────────────────

    private val _newNotification = MutableSharedFlow<Notification>(replay = 0, extraBufferCapacity = 32)
    val newNotification: SharedFlow<Notification> = _newNotification.asSharedFlow()

    private val _badgeCount = MutableStateFlow(0)
    val badgeCount: StateFlow<Int> = _badgeCount.asStateFlow()

    init {
        appScope.launch {
            notificationSignalRClient.messages.collect { envelope ->
                try {
                    when (envelope.target) {
                        "NewNotification" -> {
                            envelope.arguments?.firstOrNull()?.let { arg ->
                                val payload = json.decodeFromJsonElement(
                                    SignalRMessage.NewNotificationPayload.serializer(), arg
                                )
                                _newNotification.emit(
                                    Notification(
                                        notificationId = payload.notificationId ?: "",
                                        type = payload.type ?: "",
                                        title = payload.title ?: "",
                                        body = payload.body,
                                        deepLinkPayload = payload.deepLinkPayload,
                                        isRead = payload.isRead ?: false,
                                        createdAt = payload.createdAt ?: ""
                                    )
                                )
                            }
                        }
                        "BadgeUpdate" -> {
                            envelope.arguments?.firstOrNull()?.let { arg ->
                                val payload = json.decodeFromJsonElement(
                                    SignalRMessage.BadgeUpdatePayload.serializer(), arg
                                )
                                _badgeCount.value = payload.unreadCount
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

    override suspend fun getNotifications(cursor: String?): Result<Pair<List<Notification>, String?>> =
        withTokenRetry {
            val res = apiService.getNotifications(cursor)
            val notifications = res.notifications.map { dto ->
                Notification(
                    notificationId = dto.notificationId,
                    type = dto.type,
                    title = dto.title,
                    body = dto.body,
                    deepLinkPayload = dto.deepLinkPayload,
                    isRead = dto.isRead,
                    createdAt = dto.createdAt
                )
            }
            // Seed badge count from first load if not yet set
            if (cursor == null) {
                _badgeCount.value = notifications.count { !it.isRead }
            }
            notifications to res.nextCursor
        }

    override suspend fun registerDeviceToken(token: String): Result<Unit> =
        withTokenRetry {
            apiService.registerDeviceToken(RegisterDeviceTokenRequest(token, "Android"))
            Unit
        }

    override suspend fun markRead(notificationIds: List<String>?): Result<Int> =
        withTokenRetry {
            val res = apiService.markNotificationsRead(notificationIds)
            if (notificationIds == null) _badgeCount.value = 0
            res.markedCount
        }

    override suspend fun updateOptOut(type: String, optOut: Boolean): Result<Unit> =
        withTokenRetry {
            apiService.updateOptOut(UpdateOptOutRequest(type, optOut))
            Unit
        }

    // ── Connection lifecycle ───────────────────────────────────────────

    override suspend fun connectNotificationHub() {
        appScope.launch {
            try {
                notificationSignalRClient.connect()
            } catch (_: Exception) {
                // Reconnect handled by SignalRClient internally
            }
        }
    }

    override suspend fun disconnectNotificationHub() {
        notificationSignalRClient.disconnect()
    }
}
