package com.dinder.app.domain.repository

import com.dinder.app.domain.model.User

/**
 * Repository for authentication operations.
 */
interface AuthRepository {
    suspend fun login(email: String, password: String): Result<User>
    suspend fun register(email: String, password: String, birthday: String? = null): Result<User>
    suspend fun externalLogin(email: String, provider: String, providerUserId: String): Result<User>
    suspend fun refreshToken(): Result<Unit>
    suspend fun deleteAccount(): Result<Unit>
    suspend fun logout()
    suspend fun isLoggedIn(): Boolean
    suspend fun restoreSession(): Boolean
}
