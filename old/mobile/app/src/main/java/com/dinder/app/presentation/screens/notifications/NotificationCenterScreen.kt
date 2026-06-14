package com.dinder.app.presentation.screens.notifications

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Campaign
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Favorite
import androidx.compose.material.icons.filled.MailOutline
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import com.dinder.app.domain.model.Notification

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NotificationCenterScreen(
    onNavigateBack: () -> Unit,
    onNavigateToChat: (String) -> Unit,
    viewModel: NotificationViewModel = hiltViewModel()
) {
    val state by viewModel.state.collectAsState()
    val badgeCount by viewModel.badgeCount.collectAsState()
    val listState = rememberLazyListState()

    // Handle deep-link navigation
    LaunchedEffect(Unit) {
        viewModel.navigateToChat.collect { convId -> onNavigateToChat(convId) }
    }

    // Infinite scroll
    val shouldLoadMore = remember {
        derivedStateOf {
            val lastVisible = listState.layoutInfo.visibleItemsInfo.lastOrNull()?.index ?: 0
            lastVisible >= listState.layoutInfo.totalItemsCount - 3
        }
    }
    LaunchedEffect(shouldLoadMore.value) {
        if (shouldLoadMore.value && state.nextCursor != null && !state.isLoadingMore) {
            viewModel.loadNotifications(state.nextCursor)
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Notifications") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back")
                    }
                },
                actions = {
                    if (badgeCount > 0) {
                        TextButton(onClick = { viewModel.markAllRead() }) {
                            Text("Mark all read")
                        }
                    }
                }
            )
        }
    ) { innerPadding ->
        Box(modifier = Modifier.fillMaxSize().padding(innerPadding)) {
            when {
                state.isLoading -> {
                    CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
                }
                state.error != null && state.notifications.isEmpty() -> {
                    Column(
                        modifier = Modifier.align(Alignment.Center),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text("Could not load notifications", color = MaterialTheme.colorScheme.error)
                        Spacer(Modifier.height(8.dp))
                        TextButton(onClick = { viewModel.loadNotifications() }) { Text("Retry") }
                    }
                }
                state.notifications.isEmpty() -> {
                    Column(
                        modifier = Modifier.align(Alignment.Center),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Icon(
                            Icons.Default.Notifications,
                            contentDescription = null,
                            modifier = Modifier.size(64.dp),
                            tint = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.4f)
                        )
                        Spacer(Modifier.height(16.dp))
                        Text("No notifications yet", style = MaterialTheme.typography.titleMedium)
                    }
                }
                else -> {
                    LazyColumn(state = listState, modifier = Modifier.fillMaxSize()) {
                        // Opt-out toggles section
                        item {
                            OptOutSection(
                                optOutMatch = state.optOutMatch,
                                optOutMessage = state.optOutMessage,
                                optOutPromotional = state.optOutPromotional,
                                onToggleMatch = { viewModel.updateOptOut("Match", it) },
                                onToggleMessage = { viewModel.updateOptOut("Message", it) },
                                onTogglePromotional = { viewModel.updateOptOut("Promotional", it) }
                            )
                            HorizontalDivider(modifier = Modifier.padding(horizontal = 16.dp))
                        }

                        items(state.notifications, key = { it.notificationId }) { notification ->
                            NotificationRow(
                                notification = notification,
                                onClick = { viewModel.onNotificationTap(notification) }
                            )
                        }

                        if (state.isLoadingMore) {
                            item {
                                Box(Modifier.fillMaxWidth().padding(16.dp), contentAlignment = Alignment.Center) {
                                    CircularProgressIndicator(modifier = Modifier.size(24.dp))
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun NotificationRow(notification: Notification, onClick: () -> Unit) {
    val icon = when (notification.type) {
        "Match" -> Icons.Default.Favorite
        "Message" -> Icons.Default.MailOutline
        else -> Icons.Default.Campaign
    }
    val iconTint = when (notification.type) {
        "Match" -> MaterialTheme.colorScheme.primary
        "Message" -> MaterialTheme.colorScheme.secondary
        else -> MaterialTheme.colorScheme.tertiary
    }

    Surface(
        modifier = Modifier.fillMaxWidth().clickable(onClick = onClick),
        color = if (notification.isRead) MaterialTheme.colorScheme.surface
                else MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f)
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 12.dp),
            verticalAlignment = Alignment.Top
        ) {
            Icon(
                icon, contentDescription = null,
                tint = iconTint,
                modifier = Modifier.size(24.dp)
            )
            Spacer(Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    notification.title,
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = if (!notification.isRead) FontWeight.SemiBold else FontWeight.Normal,
                    maxLines = 1, overflow = TextOverflow.Ellipsis
                )
                if (notification.body != null) {
                    Spacer(Modifier.height(2.dp))
                    Text(
                        notification.body,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 2, overflow = TextOverflow.Ellipsis
                    )
                }
                Spacer(Modifier.height(4.dp))
                Text(
                    formatTimestamp(notification.createdAt),
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.6f)
                )
            }
            if (!notification.isRead) {
                Spacer(Modifier.width(8.dp))
                Icon(
                    Icons.Default.CheckCircle,
                    contentDescription = "Unread",
                    tint = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.size(12.dp).padding(top = 4.dp)
                )
            }
        }
    }
    HorizontalDivider(modifier = Modifier.padding(start = 52.dp))
}

@Composable
private fun OptOutSection(
    optOutMatch: Boolean,
    optOutMessage: Boolean,
    optOutPromotional: Boolean,
    onToggleMatch: (Boolean) -> Unit,
    onToggleMessage: (Boolean) -> Unit,
    onTogglePromotional: (Boolean) -> Unit
) {
    Column(modifier = Modifier.padding(horizontal = 16.dp, vertical = 12.dp)) {
        Text(
            "Notification preferences",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.SemiBold
        )
        Spacer(Modifier.height(8.dp))
        OptOutToggle("Match notifications", Icons.Default.Favorite, !optOutMatch, onToggleMatch)
        OptOutToggle("Message notifications", Icons.Default.MailOutline, !optOutMessage, onToggleMessage)
        OptOutToggle("Promotional", Icons.Default.Campaign, !optOutPromotional, onTogglePromotional)
    }
}

@Composable
private fun OptOutToggle(label: String, icon: ImageVector, enabled: Boolean, onToggle: (Boolean) -> Unit) {
    Row(
        modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(icon, contentDescription = null, modifier = Modifier.size(20.dp),
            tint = MaterialTheme.colorScheme.onSurfaceVariant)
        Spacer(Modifier.width(12.dp))
        Text(label, modifier = Modifier.weight(1f), style = MaterialTheme.typography.bodyMedium)
        Switch(checked = enabled, onCheckedChange = { onToggle(!it) })
    }
}

/** Crude relative timestamp from ISO date string. */
private fun formatTimestamp(isoDate: String): String {
    if (isoDate.isBlank()) return ""
    return try {
        val instant = java.time.Instant.parse(isoDate)
        val now = java.time.Instant.now()
        val diff = java.time.Duration.between(instant, now)
        when {
            diff.toMinutes() < 1 -> "Just now"
            diff.toMinutes() < 60 -> "${diff.toMinutes()}m ago"
            diff.toHours() < 24 -> "${diff.toHours()}h ago"
            diff.toDays() < 7 -> "${diff.toDays()}d ago"
            else -> {
                val zdt = java.time.ZonedDateTime.ofInstant(instant, java.time.ZoneId.systemDefault())
                zdt.toLocalDate().toString()
            }
        }
    } catch (_: Exception) { isoDate }
}
