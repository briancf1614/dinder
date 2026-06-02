package com.dinder.app.data.repository

import com.dinder.app.data.local.TokenStorage
import com.dinder.app.data.remote.ApiService
import com.dinder.app.di.TokenRefreshedException
import com.dinder.app.data.remote.dto.LoginRequest
import com.dinder.app.data.remote.dto.RegisterRequest
import com.dinder.app.domain.model.User
import com.dinder.app.domain.repository.AuthRepository
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRepositoryImpl @Inject constructor(
    private val apiService: ApiService,
    private val tokenStorage: TokenStorage
) : AuthRepository {

    override suspend fun login(email: String, password: String): Result<User> =
        runCatching {
            val res = apiService.login(LoginRequest(email, password))
            tokenStorage.saveTokens(res.accessToken, res.refreshToken)
            User(id = res.userId, email = email)
        }

    override suspend fun register(email: String, password: String, birthday: String?): Result<User> =
        runCatching {
            val res = apiService.register(RegisterRequest(email, password, birthday))
            tokenStorage.saveTokens(res.accessToken, res.refreshToken)
            User(id = res.userId, email = email)
        }

    override suspend fun externalLogin(email: String, provider: String, providerUserId: String): Result<User> =
        runCatching {
            val res = apiService.externalLogin(
                com.dinder.app.data.remote.dto.ExternalLoginRequest(email, provider, providerUserId)
            )
            tokenStorage.saveTokens(res.accessToken, res.refreshToken)
            User(id = res.userId, email = email)
        }

    override suspend fun refreshToken(): Result<Unit> = runCatching {
        tokenStorage.getRefreshToken()?.let { rt ->
            val res = apiService.refresh(com.dinder.app.data.remote.dto.RefreshRequest(rt))
            tokenStorage.saveTokens(res.accessToken, res.refreshToken)
        } ?: throw IllegalStateException("No refresh token stored")
    }

    override suspend fun deleteAccount(): Result<Unit> = runCatching {
        apiService.deleteAccount()
        tokenStorage.clearTokens()
    }

    override suspend fun logout() = tokenStorage.clearTokens()

    override suspend fun isLoggedIn(): Boolean = tokenStorage.hasValidTokens()

    override suspend fun restoreSession(): Boolean {
        if (!tokenStorage.hasValidTokens()) return false
        val refreshed = refreshToken()
        return refreshed.isSuccess
    }
}
