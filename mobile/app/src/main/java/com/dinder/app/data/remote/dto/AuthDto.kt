package com.dinder.app.data.remote.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

// ── Request DTOs ──────────────────────────────────────────────────────

@Serializable
data class LoginRequest(
    val email: String,
    val password: String
)

@Serializable
data class RegisterRequest(
    val email: String,
    val password: String,
    val birthday: String? = null // yyyy-MM-dd
)

@Serializable
data class ExternalLoginRequest(
    val email: String,
    val provider: String,   // "Google", "Apple"
    @SerialName("providerUserId")
    val providerUserId: String
)

@Serializable
data class RefreshRequest(
    @SerialName("refreshToken")
    val refreshToken: String
)

// ── Response DTOs ─────────────────────────────────────────────────────

@Serializable
data class AuthResponse(
    @SerialName("userId")
    val userId: String,
    @SerialName("accessToken")
    val accessToken: String,
    @SerialName("refreshToken")
    val refreshToken: String
)

@Serializable
data class RefreshResponse(
    @SerialName("accessToken")
    val accessToken: String,
    @SerialName("refreshToken")
    val refreshToken: String
)

@Serializable
data class ApiError(
    val error: String,
    val errors: List<String>? = null
)
