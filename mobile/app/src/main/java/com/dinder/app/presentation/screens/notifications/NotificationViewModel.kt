package com.dinder.app.presentation.screens.notifications

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.dinder.app.data.repository.NotificationRepositoryImpl
import com.dinder.app.domain.model.Notification
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class NotificationViewModel @Inject constructor(
    private val notificationRepository: NotificationRepositoryImpl
) : ViewModel() {

    data class UiState(
        val notifications: List<Notification> = emptyList(),
        val isLoading: Boolean = false,
        val isLoadingMore: Boolean = false,
        val error: String? = null,
        val nextCursor: String? = null,
        val optOutMatch: Boolean = false,
        val optOutMessage: Boolean = false,
        val optOutPromotional: Boolean = false
    )

    private val _state = MutableStateFlow(UiState())
    val state: StateFlow<UiState> = _state.asStateFlow()

    val badgeCount: StateFlow<Int> = notificationRepository.badgeCount

    private val _navigateToChat = MutableSharedFlow<String>()
    val navigateToChat: SharedFlow<String> = _navigateToChat.asSharedFlow()

    init {
        // Collect real-time notifications
        viewModelScope.launch {
            notificationRepository.newNotification.collect { notification ->
                val current = _state.value.notifications.toMutableList()
                current.add(0, notification)
                _state.value = _state.value.copy(notifications = current)
            }
        }

        connectAndLoad()
    }

    fun connectAndLoad() {
        viewModelScope.launch { notificationRepository.connectNotificationHub() }
        loadNotifications()
    }

    fun loadNotifications(cursor: String? = null) {
        viewModelScope.launch {
            _state.value = _state.value.copy(
                isLoading = cursor == null,
                isLoadingMore = cursor != null,
                error = null
            )
            notificationRepository.getNotifications(cursor)
                .onSuccess { (notifications, nextCursor) ->
                    _state.value = _state.value.copy(
                        notifications = if (cursor == null) notifications
                                        else _state.value.notifications + notifications,
                        isLoading = false, isLoadingMore = false, nextCursor = nextCursor
                    )
                }
                .onFailure { e ->
                    _state.value = _state.value.copy(
                        isLoading = false, isLoadingMore = false, error = e.message
                    )
                }
        }
    }

    fun markAllRead() {
        viewModelScope.launch {
            notificationRepository.markRead(null)
                .onSuccess {
                    _state.value = _state.value.copy(
                        notifications = _state.value.notifications.map { it.copy(isRead = true) }
                    )
                }
        }
    }

    fun onNotificationTap(notification: Notification) {
        // Mark single notification as read
        if (!notification.isRead) {
            viewModelScope.launch {
                notificationRepository.markRead(listOf(notification.notificationId))
                _state.value = _state.value.copy(
                    notifications = _state.value.notifications.map {
                        if (it.notificationId == notification.notificationId) it.copy(isRead = true) else it
                    }
                )
            }
        }

        // Deep-link: navigate to conversation if payload present
        notification.deepLinkPayload?.let { conversationId ->
            viewModelScope.launch { _navigateToChat.emit(conversationId) }
        }
    }

    fun updateOptOut(type: String, optOut: Boolean) {
        viewModelScope.launch {
            notificationRepository.updateOptOut(type, optOut)
                .onSuccess {
                    _state.value = when (type) {
                        "Match" -> _state.value.copy(optOutMatch = optOut)
                        "Message" -> _state.value.copy(optOutMessage = optOut)
                        "Promotional" -> _state.value.copy(optOutPromotional = optOut)
                        else -> _state.value
                    }
                }
        }
    }

    fun disconnectHub() {
        viewModelScope.launch { notificationRepository.disconnectNotificationHub() }
    }
}
