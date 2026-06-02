package com.dinder.app.presentation.theme

import android.app.Activity
import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalView
import androidx.core.view.WindowCompat

private val LightColorScheme = lightColorScheme(
    primary = Coral,
    onPrimary = Color.White,
    primaryContainer = CoralContainer,
    onPrimaryContainer = CoralDark,
    secondary = Teal,
    onSecondary = Color.White,
    secondaryContainer = TealContainer,
    onSecondaryContainer = TealDark,
    tertiary = SwipeSuperLike,
    background = SurfaceLight,
    onBackground = NeutralDark,
    surface = Color.White,
    onSurface = NeutralDark,
    surfaceVariant = Color(0xFFF0F0F5),
    onSurfaceVariant = NeutralMedium,
    outline = NeutralLight,
    error = Error,
    onError = Color.White
)

private val DarkColorScheme = darkColorScheme(
    primary = Coral,
    onPrimary = Color.White,
    primaryContainer = Color(0xFF5A2020),
    onPrimaryContainer = CoralLight,
    secondary = Teal,
    onSecondary = Color.White,
    secondaryContainer = Color(0xFF1A4A47),
    onSecondaryContainer = TealLight,
    tertiary = SwipeSuperLike,
    background = SurfaceDark,
    onBackground = DarkOnSurface,
    surface = DarkSurface,
    onSurface = DarkOnSurface,
    surfaceVariant = DarkSurfaceVariant,
    onSurfaceVariant = NeutralLight,
    outline = NeutralMedium,
    error = Error,
    onError = Color.White
)

@Composable
fun DinderTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    val view = LocalView.current
    if (!view.isInEditMode) {
        SideEffect {
            val window = (view.context as Activity).window
            window.statusBarColor = colorScheme.background.toArgb()
            WindowCompat.getInsetsController(window, view).isAppearanceLightStatusBars = !darkTheme
        }
    }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = DinderTypography,
        content = content
    )
}
