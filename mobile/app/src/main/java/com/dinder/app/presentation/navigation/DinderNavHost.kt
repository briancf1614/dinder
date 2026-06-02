package com.dinder.app.presentation.navigation

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Favorite
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import com.dinder.app.presentation.screens.auth.AuthViewModel
import com.dinder.app.presentation.screens.chat.MatchesScreen
import com.dinder.app.presentation.screens.discovery.DiscoverScreen
import com.dinder.app.presentation.screens.notifications.NotificationViewModel
import com.dinder.app.presentation.screens.profile.ProfileScreen

sealed class Screen(
    val route: String,
    val label: String,
    val icon: ImageVector
) {
    data object Discover : Screen("discover", "Discover", Icons.Default.Search)
    data object Matches : Screen("matches", "Matches", Icons.Default.Favorite)
    data object Profile : Screen("profile", "Profile", Icons.Default.Person)
}

val bottomNavItems = listOf(Screen.Discover, Screen.Matches, Screen.Profile)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DinderNavHost() {
    val navController = rememberNavController()
    val authViewModel: AuthViewModel = hiltViewModel()

    val sessionChecked by authViewModel.sessionChecked.collectAsState()
    val sessionValid by authViewModel.sessionValid.collectAsState()

    // Session persistence: check stored tokens on cold start
    LaunchedEffect(Unit) {
        authViewModel.checkSession()
    }

    // Handle logout from settings/profile
    LaunchedEffect(Unit) {
        authViewModel.loggedOut.collect {
            navController.navigate("auth/login") {
                popUpTo(0) { inclusive = true }
            }
        }
    }

    // Loading state while checking session
    if (!sessionChecked) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            CircularProgressIndicator()
        }
        return
    }

    val startDestination = if (sessionValid) Screen.Discover.route else "auth/login"

    val navBackStackEntry by navController.currentBackStackEntryAsState()
    val currentDestination = navBackStackEntry?.destination

    // Notification badge state
    val notificationViewModel: NotificationViewModel = hiltViewModel()
    val badgeCount by notificationViewModel.badgeCount.collectAsState()

    val showBottomBar = bottomNavItems.any { screen ->
        currentDestination?.hierarchy?.any { it.route == screen.route } == true
    }

    Scaffold(
        bottomBar = {
            if (showBottomBar) {
                NavigationBar {
                    bottomNavItems.forEach { screen ->
                        val selected = currentDestination?.hierarchy?.any { it.route == screen.route } == true
                        NavigationBarItem(
                            icon = {
                                if (screen == Screen.Matches && badgeCount > 0) {
                                    BadgedBox(badge = { Badge { Text(badgeCount.toString()) } }) {
                                        Icon(screen.icon, contentDescription = screen.label)
                                    }
                                } else {
                                    Icon(screen.icon, contentDescription = screen.label)
                                }
                            },
                            label = { Text(screen.label) },
                            selected = selected,
                            onClick = {
                                navController.navigate(screen.route) {
                                    popUpTo(navController.graph.findStartDestination().id) {
                                        saveState = true
                                    }
                                    launchSingleTop = true
                                    restoreState = true
                                }
                            }
                        )
                    }
                }
            }
        }
    ) { innerPadding ->
        NavHost(
            navController = navController,
            startDestination = startDestination,
            modifier = Modifier.padding(innerPadding)
        ) {
            // Main screens
            composable(Screen.Discover.route) {
                DiscoverScreen()
            }
            composable(Screen.Matches.route) {
                MatchesScreen(
                    onConversationClick = { conversation ->
                        navController.navigate("main/chat/${conversation.conversationId}")
                    }
                )
            }
            composable(Screen.Profile.route) {
                ProfileScreen(
                    onNavigateToSettings = { navController.navigate("main/settings") },
                    onNavigateToNotifications = { navController.navigate("main/notifications") }
                )
            }

            // Auth graph — login/register with animations
            authNavGraph(
                navController = navController,
                onLoginSuccess = {
                    authViewModel.checkSession()
                    navController.navigate(Screen.Discover.route) {
                        popUpTo(0) { inclusive = true }
                    }
                }
            )

            // Main graph — sub-screens
            mainNavGraph(navController)
        }
    }
}
