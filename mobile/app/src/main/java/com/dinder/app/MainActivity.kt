package com.dinder.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import com.dinder.app.data.local.PreferencesStore
import com.dinder.app.presentation.navigation.DinderNavHost
import com.dinder.app.presentation.theme.DinderTheme
import dagger.hilt.android.AndroidEntryPoint
import javax.inject.Inject

@AndroidEntryPoint
class MainActivity : ComponentActivity() {

    @Inject lateinit var preferencesStore: PreferencesStore

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            val darkThemePref by preferencesStore.isDarkTheme.collectAsState(initial = null)
            val systemDark = isSystemInDarkTheme()
            val useDarkTheme = darkThemePref ?: systemDark

            DinderTheme(darkTheme = useDarkTheme) {
                DinderNavHost()
            }
        }
    }
}
