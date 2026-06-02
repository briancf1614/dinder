package com.dinder.app.presentation.screens.profile

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp

@Composable
fun ProfileScreen(
    onNavigateToSettings: () -> Unit,
    onNavigateToNotifications: () -> Unit = {},
    viewModel: ProfileViewModel = androidx.hilt.navigation.compose.hiltViewModel()
) {
    val state by viewModel.state.collectAsState()

    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        // Avatar + Tier badge
        Box {
            Box(
                Modifier.size(100.dp).clip(CircleShape)
                    .background(MaterialTheme.colorScheme.primaryContainer),
                contentAlignment = Alignment.Center
            ) {
                Icon(Icons.Default.Person, contentDescription = null, Modifier.size(48.dp), tint = MaterialTheme.colorScheme.onPrimaryContainer)
            }
            Surface(
                Modifier.align(Alignment.BottomEnd).offset(x = (-4).dp, y = (-4).dp),
                shape = RoundedCornerShape(12.dp),
                color = MaterialTheme.colorScheme.tertiary
            ) {
                Text(state.tier, modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
                    style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onTertiary)
            }
        }

        Spacer(Modifier.height(16.dp))
        Text(state.email, style = MaterialTheme.typography.titleMedium)
        Spacer(Modifier.height(32.dp))

        // Photo grid placeholder
        Text("Photos", style = MaterialTheme.typography.titleSmall, modifier = Modifier.fillMaxWidth().padding(bottom = 8.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            repeat(3) {
                Box(Modifier.weight(1f).aspectRatio(1f).clip(RoundedCornerShape(12.dp))
                    .background(MaterialTheme.colorScheme.surfaceVariant),
                    contentAlignment = Alignment.Center
                ) { Icon(Icons.Default.Add, contentDescription = "Add photo", tint = MaterialTheme.colorScheme.onSurfaceVariant) }
            }
        }
        Spacer(Modifier.height(24.dp))

        // Menu items
        ProfileMenuItem(Icons.Default.Notifications, "Notifications") { onNavigateToNotifications() }
        ProfileMenuItem(Icons.Default.Settings, "Settings") { onNavigateToSettings() }
        ProfileMenuItem(Icons.Default.Info, "About") { }
    }

    // Delete confirmation dialog
    if (state.showDeleteConfirmation) {
        AlertDialog(
            onDismissRequest = viewModel::dismissDeleteDialog,
            title = { Text("Delete Account") },
            text = { Text("This action is permanent. All your data, matches, and messages will be removed.") },
            confirmButton = {
                TextButton(onClick = viewModel::deleteAccount, enabled = !state.isLoading) {
                    Text("Delete", color = MaterialTheme.colorScheme.error)
                }
            },
            dismissButton = { TextButton(onClick = viewModel::dismissDeleteDialog) { Text("Cancel") } }
        )
    }
}

@Composable
private fun ProfileMenuItem(icon: ImageVector, label: String, onClick: () -> Unit) {
    Surface(
        onClick = onClick,
        modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
        shape = RoundedCornerShape(12.dp)
    ) {
        Row(Modifier.padding(16.dp), verticalAlignment = Alignment.CenterVertically) {
            Icon(icon, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.width(16.dp))
            Text(label, style = MaterialTheme.typography.bodyLarge)
        }
    }
}
