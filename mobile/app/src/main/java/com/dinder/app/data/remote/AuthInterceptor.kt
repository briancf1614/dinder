package com.dinder.app.data.remote

import com.dinder.app.data.remote.dto.RefreshRequest
import com.dinder.app.data.remote.dto.RefreshResponse
import com.dinder.app.data.local.TokenStorage
import com.dinder.app.di.BaseUrl
import io.ktor.client.*
import io.ktor.client.call.body
import io.ktor.client.engine.cio.*
import io.ktor.client.plugins.*
import io.ktor.client.plugins.contentnegotiation.*
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.http.*
import io.ktor.serialization.kotlinx.json.*
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.serialization.json.Json
import javax.inject.Inject
import javax.inject.Singleton

/** Event emitted when the session expires (refresh fails) — navigation should redirect to login. */
object SessionExpired

/**
 * Ktor plugin that:
 * 1. Attaches JWT Bearer header to every request.
 * 2. On 401, refreshes the token via POST /identity/refresh.
 * 3. On refresh failure, clears tokens and emits [SessionExpired].
 *
 * The caller (HttpResponseValidator) is responsible for retrying the original request
 * after a successful refresh.
 */
@Singleton
class AuthInterceptor @Inject constructor(
    private val tokenStorage: TokenStorage,
    private val json: Json,
    @BaseUrl private val baseUrl: String
) {
    private val _sessionExpired = MutableSharedFlow<SessionExpired>(replay = 0)
    val sessionExpired: SharedFlow<SessionExpired> = _sessionExpired

    /**
     * Called before each request by the HttpClient's request pipeline.
     * Attaches the JWT access token as a Bearer header.
     */
    suspend fun prepareRequest(builder: HttpRequestBuilder) {
        val token = tokenStorage.getAccessToken()
        if (token != null) {
            builder.headers[HttpHeaders.Authorization] = "Bearer $token"
        }
    }

    /**
     * Handles a 401 response: attempts token refresh using a separate,
     * non-intercepted HttpClient to avoid circular auth.
     *
     * @return true if refresh succeeded and tokens were updated (caller should retry).
     *         false if refresh failed (session expired — tokens cleared, event emitted).
     */
    suspend fun handleUnauthorized(): Boolean {
        val refreshToken = tokenStorage.getRefreshToken() ?: run {
            emitSessionExpired()
            return false
        }

        return try {
            // Lightweight client WITHOUT auth interceptor to avoid recursion
            val refreshClient = HttpClient(CIO) {
                install(ContentNegotiation) {
                    json(this@AuthInterceptor.json)
                }
                install(HttpTimeout) {
                    requestTimeoutMillis = 10_000
                    connectTimeoutMillis = 5_000
                }
            }
            try {
                val response: RefreshResponse = refreshClient.post("$baseUrl/api/v1/identity/refresh") {
                    contentType(ContentType.Application.Json)
                    setBody(RefreshRequest(refreshToken))
                }.body<RefreshResponse>()
                tokenStorage.saveTokens(response.accessToken, response.refreshToken)
                true
            } finally {
                refreshClient.close()
            }
        } catch (e: Exception) {
            emitSessionExpired()
            false
        }
    }

    /** Notify navigation that the session has expired. */
    suspend fun emitSessionExpired() {
        tokenStorage.clearTokens()
        _sessionExpired.emit(SessionExpired)
    }
}
