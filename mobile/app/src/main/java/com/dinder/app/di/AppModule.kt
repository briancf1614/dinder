package com.dinder.app.di

import com.dinder.app.data.local.PreferencesStore
import com.dinder.app.data.local.TokenStorage
import com.dinder.app.data.remote.ApiService
import com.dinder.app.data.remote.AuthInterceptor
import com.dinder.app.data.remote.SignalRClient
import com.dinder.app.data.repository.AuthRepositoryImpl
import com.dinder.app.data.repository.ChatRepositoryImpl
import com.dinder.app.data.repository.DiscoveryRepositoryImpl
import com.dinder.app.data.repository.NotificationRepositoryImpl
import com.dinder.app.domain.repository.AuthRepository
import com.dinder.app.domain.repository.ChatRepository
import com.dinder.app.domain.repository.DiscoveryRepository
import com.dinder.app.domain.repository.NotificationRepository
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import io.ktor.client.*
import io.ktor.client.engine.cio.*
import io.ktor.client.plugins.*
import io.ktor.client.plugins.contentnegotiation.*
import io.ktor.client.plugins.logging.*
import io.ktor.client.plugins.websocket.*
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.http.*
import io.ktor.serialization.kotlinx.json.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.serialization.json.Json
import javax.inject.Named
import javax.inject.Qualifier
import javax.inject.Singleton

@Qualifier
@Retention(AnnotationRetention.BINARY)
annotation class BaseUrl

@Qualifier
@Retention(AnnotationRetention.BINARY)
annotation class ChatHubUrl

@Qualifier
@Retention(AnnotationRetention.BINARY)
annotation class NotificationHubUrl

@Module
@InstallIn(SingletonComponent::class)
object AppModule {

    @Provides
    @BaseUrl
    fun provideBaseUrl(): String = "http://10.0.2.2:5000" // Android emulator localhost

    @Provides
    @ChatHubUrl
    fun provideChatHubUrl(@BaseUrl baseUrl: String): String = "$baseUrl/hubs/chat"

    @Provides
    @NotificationHubUrl
    fun provideNotificationHubUrl(@BaseUrl baseUrl: String): String = "$baseUrl/hubs/notifications"

    @Provides
    @Singleton
    fun provideJson(): Json = Json {
        ignoreUnknownKeys = true
        isLenient = true
        encodeDefaults = true
        coerceInputValues = true
    }

    @Provides
    @Singleton
    fun provideAuthInterceptor(
        tokenStorage: TokenStorage,
        json: Json,
        @BaseUrl baseUrl: String
    ): AuthInterceptor = AuthInterceptor(tokenStorage, json, baseUrl)

    @Provides
    @Singleton
    fun provideHttpClient(
        json: Json,
        authInterceptor: AuthInterceptor
    ): HttpClient {
        val client = HttpClient(CIO) {
            install(ContentNegotiation) {
                json(json)
            }

            install(Logging) {
                level = LogLevel.BODY
            }

            install(WebSockets)

            // Default request config
            defaultRequest {
                contentType(ContentType.Application.Json)
                accept(ContentType.Application.Json)
            }

            // Timeouts
            install(HttpTimeout) {
                requestTimeoutMillis = 30_000
                connectTimeoutMillis = 15_000
                socketTimeoutMillis = 30_000
            }

            // Handle 401 — attempt token refresh, emit SessionExpired on failure
            HttpResponseValidator {
                validateResponse { response ->
                    if (response.status == HttpStatusCode.Unauthorized) {
                        val refreshed = authInterceptor.handleUnauthorized()
                        if (refreshed) {
                            // Tokens refreshed — caller should retry with new token
                            throw TokenRefreshedException()
                        } else {
                            // Refresh failed — session is dead
                            throw ClientRequestException(response, "Session expired")
                        }
                    }
                }
            }
        }

        // Attach JWT auth header before every request via the client's request pipeline.
        // Intercepted AFTER construction to access the pipeline.
        client.requestPipeline.intercept(HttpRequestPipeline.State) {
            authInterceptor.prepareRequest(context)
        }

        return client
    }

    @Provides
    @Singleton
    fun provideApiService(
        httpClient: HttpClient,
        @BaseUrl baseUrl: String
    ): ApiService = ApiService(httpClient, baseUrl)

    @Provides
    @Singleton
    fun provideApplicationScope(): CoroutineScope =
        CoroutineScope(SupervisorJob() + Dispatchers.Default)

    @Provides
    @Named("chat")
    @Singleton
    fun provideChatSignalRClient(
        httpClient: HttpClient,
        tokenStorage: TokenStorage,
        @ChatHubUrl hubUrl: String,
        scope: CoroutineScope
    ): SignalRClient = SignalRClient(
        httpClient = httpClient,
        hubRelativePath = hubUrl,
        tokenStorage = tokenStorage,
        scope = scope
    )

    @Provides
    @Named("notification")
    @Singleton
    fun provideNotificationSignalRClient(
        httpClient: HttpClient,
        tokenStorage: TokenStorage,
        @NotificationHubUrl hubUrl: String,
        scope: CoroutineScope
    ): SignalRClient = SignalRClient(
        httpClient = httpClient,
        hubRelativePath = hubUrl,
        tokenStorage = tokenStorage,
        scope = scope
    )

    // ── Repository bindings ─────────────────────────────────────────

    @Provides
    @Singleton
    fun provideAuthRepository(
        apiService: ApiService,
        tokenStorage: TokenStorage
    ): AuthRepository = AuthRepositoryImpl(apiService, tokenStorage)

    @Provides
    @Singleton
    fun provideDiscoveryRepository(
        apiService: ApiService
    ): DiscoveryRepository = DiscoveryRepositoryImpl(apiService)

    @Provides
    @Singleton
    fun provideChatRepository(
        apiService: ApiService,
        @Named("chat") chatSignalRClient: SignalRClient,
        scope: CoroutineScope
    ): ChatRepositoryImpl = ChatRepositoryImpl(apiService, chatSignalRClient, scope)

    @Provides
    @Singleton
    fun provideNotificationRepository(
        apiService: ApiService,
        @Named("notification") notificationSignalRClient: SignalRClient,
        scope: CoroutineScope
    ): NotificationRepositoryImpl = NotificationRepositoryImpl(apiService, notificationSignalRClient, scope)
}

/**
 * Thrown inside [HttpResponseValidator] when a 401 triggered a successful token refresh.
 * The request was NOT retried automatically — calling code should reissue the request
 * with the updated token. Repositories handle this in PR 2.
 */
class TokenRefreshedException : Exception("Token refreshed — retry the request")
