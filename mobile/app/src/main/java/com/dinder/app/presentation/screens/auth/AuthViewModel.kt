package com.dinder.app.presentation.screens.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
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
class AuthViewModel @Inject constructor(
    private val authRepository: AuthRepository
) : ViewModel() {

    /** Login state */
    data class LoginUiState(
        val email: String = "",
        val password: String = "",
        val isLoading: Boolean = false,
        val error: String? = null
    )

    /** Register state */
    data class RegisterUiState(
        val email: String = "",
        val password: String = "",
        val confirmPassword: String = "",
        val birthday: String? = null,
        val isLoading: Boolean = false,
        val error: String? = null
    )

    private val _loginState = MutableStateFlow(LoginUiState())
    val loginState: StateFlow<LoginUiState> = _loginState.asStateFlow()

    private val _registerState = MutableStateFlow(RegisterUiState())
    val registerState: StateFlow<RegisterUiState> = _registerState.asStateFlow()

    private val _loginSuccess = MutableSharedFlow<Unit>()
    val loginSuccess: SharedFlow<Unit> = _loginSuccess.asSharedFlow()

    private val _registerSuccess = MutableSharedFlow<Unit>()
    val registerSuccess: SharedFlow<Unit> = _registerSuccess.asSharedFlow()

    /** Session check state — used by DinderNavHost on cold start. */
    private val _sessionChecked = MutableStateFlow(false)
    val sessionChecked: StateFlow<Boolean> = _sessionChecked.asStateFlow()

    private val _sessionValid = MutableStateFlow(false)
    val sessionValid: StateFlow<Boolean> = _sessionValid.asStateFlow()

    /** Shared logout event — emitted from profile/settings for central handling. */
    private val _loggedOut = MutableSharedFlow<Unit>()
    val loggedOut: SharedFlow<Unit> = _loggedOut.asSharedFlow()

    fun checkSession() {
        viewModelScope.launch {
            val valid = authRepository.restoreSession()
            _sessionValid.value = valid
            _sessionChecked.value = true
        }
    }

    fun logout() {
        viewModelScope.launch {
            authRepository.logout()
            _sessionValid.value = false
            _loggedOut.emit(Unit)
        }
    }

    // ── Login ──────────────────────────────────────────────────────────

    fun onLoginEmailChanged(email: String) { _loginState.value = _loginState.value.copy(email = email, error = null) }
    fun onLoginPasswordChanged(pw: String) { _loginState.value = _loginState.value.copy(password = pw, error = null) }

    fun login() {
        val s = _loginState.value
        if (s.email.isBlank() || s.password.isBlank()) {
            _loginState.value = s.copy(error = "Email and password are required")
            return
        }
        viewModelScope.launch {
            _loginState.value = s.copy(isLoading = true, error = null)
            authRepository.login(s.email, s.password)
                .onSuccess { _loginSuccess.emit(Unit) }
                .onFailure { e -> _loginState.value = _loginState.value.copy(isLoading = false, error = e.message ?: "Login failed") }
        }
    }

    // ── Register ───────────────────────────────────────────────────────

    fun onRegisterEmailChanged(email: String) { _registerState.value = _registerState.value.copy(email = email, error = null) }
    fun onRegisterPasswordChanged(pw: String) { _registerState.value = _registerState.value.copy(password = pw, error = null) }
    fun onRegisterConfirmChanged(pw: String) { _registerState.value = _registerState.value.copy(confirmPassword = pw, error = null) }
    fun onRegisterBirthdayChanged(bday: String?) { _registerState.value = _registerState.value.copy(birthday = bday, error = null) }

    fun register() {
        val s = _registerState.value
        val validationError = validateRegistration(s.email, s.password, s.confirmPassword, s.birthday)
        if (validationError != null) {
            _registerState.value = s.copy(error = validationError)
            return
        }
        viewModelScope.launch {
            _registerState.value = s.copy(isLoading = true, error = null)
            authRepository.register(s.email, s.password, s.birthday)
                .onSuccess { _registerSuccess.emit(Unit) }
                .onFailure { e -> _registerState.value = _registerState.value.copy(isLoading = false, error = e.message ?: "Registration failed") }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────

    companion object {
        fun validatePassword(password: String): Boolean =
            password.length >= 8 && password.any { it.isUpperCase() } && password.any { it.isDigit() }

        fun isAge18Plus(birthday: String?): Boolean {
            if (birthday == null) return false
            return try {
                val fmt = java.text.SimpleDateFormat("yyyy-MM-dd", java.util.Locale.US)
                val eighteenAgo = java.util.Calendar.getInstance().apply { add(java.util.Calendar.YEAR, -18) }.time
                fmt.parse(birthday)!!.before(eighteenAgo)
            } catch (_: Exception) { false }
        }

        fun validateRegistration(email: String, password: String, confirm: String, birthday: String?): String? = when {
            email.isBlank() -> "Email is required"
            password.length < 8 -> "Password must be at least 8 characters"
            !password.any { it.isUpperCase() } -> "Password must contain an uppercase letter"
            !password.any { it.isDigit() } -> "Password must contain a digit"
            password != confirm -> "Passwords do not match"
            !isAge18Plus(birthday) -> "You must be at least 18 years old"
            else -> null
        }
    }
}
