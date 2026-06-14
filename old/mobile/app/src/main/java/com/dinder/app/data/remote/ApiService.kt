package com.dinder.app.data.remote

import com.dinder.app.data.remote.dto.*
import io.ktor.client.*
import io.ktor.client.call.*
import io.ktor.client.plugins.*
import io.ktor.client.plugins.contentnegotiation.*
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.http.*
import io.ktor.serialization.kotlinx.json.*
import kotlinx.serialization.json.Json
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Ktor HTTP client wrapping all Dinder REST API endpoints.
 * Base URL and JSON config injected via Hilt.
 */
@Singleton
class ApiService @Inject constructor(
    private val httpClient: HttpClient,
    private val baseUrl: String
) {
    // ── Identity ──────────────────────────────────────────────────────

    suspend fun login(request: LoginRequest): AuthResponse =
        post("$baseUrl/api/v1/identity/login", request)

    suspend fun register(request: RegisterRequest): AuthResponse =
        post("$baseUrl/api/v1/identity/register", request)

    suspend fun externalLogin(request: ExternalLoginRequest): AuthResponse =
        post("$baseUrl/api/v1/identity/login/external", request)

    suspend fun refresh(request: RefreshRequest): RefreshResponse =
        post("$baseUrl/api/v1/identity/refresh", request)

    suspend fun deleteAccount(): HttpResponse =
        httpClient.delete("$baseUrl/api/v1/identity/account")

    // ── Discovery ─────────────────────────────────────────────────────

    suspend fun getCandidates(
        latitude: Double = 0.0,
        longitude: Double = 0.0,
        cursor: String? = null,
        limit: Int = 20
    ): CandidatesResponse = get("$baseUrl/api/v1/discovery/candidates") {
        parameter("latitude", latitude)
        parameter("longitude", longitude)
        cursor?.let { parameter("cursor", it) }
        parameter("limit", limit)
    }

    suspend fun swipe(request: SwipeRequest): SwipeResponse =
        post("$baseUrl/api/v1/discovery/swipe", request)

    suspend fun getMatches(): MatchesResponse =
        get("$baseUrl/api/v1/discovery/matches")

    // ── Chat ───────────────────────────────────────────────────────────

    suspend fun getConversations(
        cursor: String? = null,
        limit: Int = 20
    ): ConversationsResponse = get("$baseUrl/api/v1/chat/conversations") {
        cursor?.let { parameter("cursor", it) }
        parameter("limit", limit)
    }

    suspend fun getMessages(
        conversationId: String,
        cursor: String? = null,
        limit: Int = 50
    ): MessagesResponse = get("$baseUrl/api/v1/chat/conversations/$conversationId/messages") {
        cursor?.let { parameter("cursor", it) }
        parameter("limit", limit)
    }

    suspend fun unmatch(conversationId: String): HttpResponse =
        httpClient.post("$baseUrl/api/v1/chat/conversations/$conversationId/unmatch")

    // ── Notifications ──────────────────────────────────────────────────

    suspend fun getNotifications(
        cursor: String? = null,
        limit: Int = 20
    ): NotificationsResponse = get("$baseUrl/api/v1/notifications") {
        cursor?.let { parameter("cursor", it) }
        parameter("limit", limit)
    }

    suspend fun registerDeviceToken(request: RegisterDeviceTokenRequest): HttpResponse =
        httpClient.post("$baseUrl/api/v1/notifications/register-token") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }

    suspend fun markNotificationsRead(notificationIds: List<String>?): MarkReadResponse =
        post("$baseUrl/api/v1/notifications/read", MarkReadRequest(notificationIds))

    suspend fun updateOptOut(request: UpdateOptOutRequest): HttpResponse =
        httpClient.put("$baseUrl/api/v1/notifications/opt-out") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }

    // ── Internal helpers ───────────────────────────────────────────────

    private suspend inline fun <reified T> get(url: String, block: HttpRequestBuilder.() -> Unit = {}): T =
        httpClient.get(url, block).body()

    private suspend inline fun <reified T> post(url: String, body: Any): T =
        httpClient.post(url) {
            contentType(ContentType.Application.Json)
            setBody(body)
        }.body()
}
