package com.dinder.app.presentation.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.navigation.NavController
import androidx.navigation.NavGraphBuilder
import androidx.navigation.NavType
import androidx.navigation.compose.composable
import androidx.navigation.navArgument
import com.dinder.app.presentation.screens.chat.ChatScreen
import com.dinder.app.presentation.screens.notifications.NotificationCenterScreen
import com.dinder.app.presentation.screens.settings.SettingsScreen

/**
 * Main navigation graph — authenticated screens beyond bottom nav:
 *  - Chat (per conversation)
 *  - Notification center
 *  - Settings
 */
object MainNavGraph {

    fun register(navGraphBuilder: NavGraphBuilder, navController: NavController) {
        navGraphBuilder.apply {
            composable(
                route = "main/chat/{conversationId}",
                arguments = listOf(navArgument("conversationId") { type = NavType.StringType })
            ) { backStackEntry ->
                val conversationId = backStackEntry.arguments?.getString("conversationId") ?: return@composable
                ChatScreen(
                    conversationId = conversationId,
                    onNavigateBack = { navController.popBackStack() }
                )
            }

            composable("main/notifications") {
                NotificationCenterScreen(
                    onNavigateBack = { navController.popBackStack() },
                    onNavigateToChat = { conversationId ->
                        navController.navigate("main/chat/$conversationId")
                    }
                )
            }

            composable("main/settings") {
                SettingsScreen(onNavigateBack = { navController.popBackStack() })
            }
        }
    }
}

fun NavGraphBuilder.mainNavGraph(navController: NavController) {
    MainNavGraph.register(this, navController)
}
