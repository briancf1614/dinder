package com.dinder.app.presentation.screens.profile

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.dinder.app.domain.model.Profile
import com.dinder.app.domain.repository.AuthRepository
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
class ProfileViewModel @Inject constructor(
    private val authRepository: AuthRepository
) : ViewModel() {

    data class UiState(
        val email: String = "",
        val tier: String = "Free",
        val isLoading: Boolean = false,
        val error: String? = null,
        val showDeleteConfirmation: Boolean = false
    )

    private val _state = MutableStateFlow(UiState())
    val state: StateFlow<UiState> = _state.asStateFlow()

    private val _loggedOut = MutableSharedFlow<Unit>()
    val loggedOut: SharedFlow<Unit> = _loggedOut.asSharedFlow()

    fun showDeleteDialog() { _state.value = _state.value.copy(showDeleteConfirmation = true) }
    fun dismissDeleteDialog() { _state.value = _state.value.copy(showDeleteConfirmation = false) }

    fun deleteAccount() {
        viewModelScope.launch {
            _state.value = _state.value.copy(isLoading = true)
            authRepository.deleteAccount()
                .onSuccess { _loggedOut.emit(Unit) }
                .onFailure { e -> _state.value = _state.value.copy(isLoading = false, error = e.message) }
        }
    }

    fun logout() {
        viewModelScope.launch {
            authRepository.logout()
            _loggedOut.emit(Unit)
        }
    }
}
