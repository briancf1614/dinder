package com.dinder.app.data.local

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

/** Extension property for DataStore creation — one per process. */
private val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "dinder_prefs")

/**
 * Jetpack DataStore for non-sensitive preferences:
 * - Theme mode (dark/light)
 * - Onboarding completion flags
 */
@Singleton
class PreferencesStore @Inject constructor(
    @ApplicationContext private val context: Context
) {
    private val dataStore = context.dataStore

    /** Whether the user prefers dark theme (null = follow system). */
    val isDarkTheme: Flow<Boolean?> = dataStore.data.map { prefs ->
        prefs[KEY_DARK_THEME]
    }

    /** Whether onboarding has been completed. */
    val onboardingComplete: Flow<Boolean> = dataStore.data.map { prefs ->
        prefs[KEY_ONBOARDING_COMPLETE] ?: false
    }

    suspend fun setDarkTheme(enabled: Boolean?) {
        dataStore.edit { prefs ->
            if (enabled != null) {
                prefs[KEY_DARK_THEME] = enabled
            } else {
                prefs.remove(KEY_DARK_THEME)
            }
        }
    }

    suspend fun setOnboardingComplete() {
        dataStore.edit { prefs ->
            prefs[KEY_ONBOARDING_COMPLETE] = true
        }
    }

    suspend fun clearAll() {
        dataStore.edit { it.clear() }
    }

    companion object {
        private val KEY_DARK_THEME = booleanPreferencesKey("dark_theme")
        private val KEY_ONBOARDING_COMPLETE = booleanPreferencesKey("onboarding_complete")
    }
}
