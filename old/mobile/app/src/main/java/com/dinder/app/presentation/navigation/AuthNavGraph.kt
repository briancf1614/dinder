package com.dinder.app.presentation.navigation

import androidx.compose.animation.*
import androidx.navigation.NavController
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import com.dinder.app.presentation.screens.auth.LoginScreen
import com.dinder.app.presentation.screens.auth.RegisterScreen

/** Auth navigation graph — login ↔ register with enter/exit animations. */
fun NavGraphBuilder.authNavGraph(
    navController: NavController,
    onLoginSuccess: () -> Unit
) {
    composable(
        "auth/login",
        enterTransition = { slideInHorizontally(initialOffsetX = { -it }) + fadeIn() },
        exitTransition = { slideOutHorizontally(targetOffsetX = { -it }) + fadeOut() }
    ) {
        LoginScreen(
            onNavigateToRegister = { navController.navigate("auth/register") },
            onLoginSuccess = onLoginSuccess
        )
    }

    composable(
        "auth/register",
        enterTransition = { slideInHorizontally(initialOffsetX = { it }) + fadeIn() },
        exitTransition = { slideOutHorizontally(targetOffsetX = { it }) + fadeOut() }
    ) {
        RegisterScreen(
            onNavigateToLogin = { navController.popBackStack() },
            onRegisterSuccess = onLoginSuccess
        )
    }
}
