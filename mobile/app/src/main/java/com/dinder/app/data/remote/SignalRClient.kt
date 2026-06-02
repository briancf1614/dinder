package com.dinder.app.data.remote

import com.dinder.app.data.local.TokenStorage
import io.ktor.client.*
import io.ktor.client.plugins.websocket.*
import io.ktor.websocket.*
import kotlinx.coroutines.*
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.*
import kotlinx.serialization.json.*
import java.util.concurrent.atomic.AtomicBoolean
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Base SignalR client implementing the JSON Hub Protocol over Ktor WebSocket.
 *
 * Handles:
 * - Handshake negotiation (`{"protocol":"json","version":1}\x1E`)
 * - Method invocation (client → server)
 * - Message receive (server → client) via Flow
 * - Auto-reconnect with exponential backoff (1s, 2s, 4s, 8s, max 30s)
 *
 * Extend or configure per hub (ChatHub, NotificationHub).
 */
class SignalRClient(
    private val httpClient: HttpClient,
    private val hubRelativePath: String,
    private val tokenStorage: TokenStorage,
    private val scope: CoroutineScope
) {
    private val json = Json { ignoreUnknownKeys = true }
    private val connected = AtomicBoolean(false)
    private val reconnectAttempt = AtomicBoolean(false)

    /** Server → client messages emitted as parsed JSON objects. */
    private val _messages = Channel<SignalRMessage.Envelope>(Channel.BUFFERED)
    val messages: Flow<SignalRMessage.Envelope> = _messages.receiveAsFlow()

    /** Raw incoming text lines (handshake / errors). */
    private val _rawMessages = MutableSharedFlow<String>(replay = 0)
    val rawMessages: SharedFlow<String> = _rawMessages

    /** Connection state for UI observation. */
    private val _connectionState = MutableStateFlow(ConnectionState.Disconnected)
    val connectionState: StateFlow<ConnectionState> = _connectionState

    private var reconnectJob: Job? = null
    private var currentSession: WebSocketSession? = null

    enum class ConnectionState { Connected, Connecting, Disconnected, Reconnecting }

    // ── Public API ──────────────────────────────────────────────────────

    /** Connect to the hub, performing handshake. Suspends until handshake completes. */
    suspend fun connect() {
        if (connected.get()) return
        _connectionState.value = ConnectionState.Connecting

        try {
            val token = tokenStorage.getAccessToken()
                ?: throw IllegalStateException("No access token — cannot connect to SignalR")

            val url = "${hubRelativePath}?access_token=$token"

            httpClient.webSocket(url) {
                currentSession = this
                connected.set(true)
                _connectionState.value = ConnectionState.Connected

                // Send handshake
                val handshake = json.encodeToString(
                    SignalRMessage.HandshakeRequest.serializer(),
                    SignalRMessage.HandshakeRequest()
                )
                send(Frame.Text(handshake + SignalRMessage.RECORD_DELIMITER))

                // Receive loop
                for (frame in incoming) {
                    when (frame) {
                        is Frame.Text -> {
                            val text = frame.readText()
                            handleTextFrame(text)
                        }
                        is Frame.Close -> break
                        else -> continue
                    }
                }
            }
        } catch (e: Exception) {
            connected.set(false)
            _connectionState.value = ConnectionState.Disconnected
            throw e
        } finally {
            onDisconnected()
        }
    }

    /** Disconnect gracefully. */
    suspend fun disconnect() {
        reconnectJob?.cancel()
        reconnectJob = null
        try {
            currentSession?.close()
        } catch (_: Exception) { }
        connected.set(false)
        currentSession = null
        _connectionState.value = ConnectionState.Disconnected
    }

    /** Invoke a SignalR method on the hub (client → server). */
    suspend fun invoke(methodName: String, vararg args: JsonElement) {
        val invocation = json.encodeToString(
            SignalRMessage.Envelope.serializer(),
            SignalRMessage.Envelope(
                type = 1,
                target = methodName,
                arguments = JsonArray(args.toList())
            )
        )
        val frame = invocation + SignalRMessage.RECORD_DELIMITER
        currentSession?.send(Frame.Text(frame))
            ?: throw IllegalStateException("Not connected")
    }

    // ── Internal ────────────────────────────────────────────────────────

    private suspend fun handleTextFrame(text: String) {
        val parts = text.split(SignalRMessage.RECORD_DELIMITER)
        for (part in parts) {
            if (part.isEmpty()) continue

            // Skip handshake response (empty JSON object `{}`)
            if (part == "{}") continue

            try {
                val envelope = json.decodeFromString(SignalRMessage.Envelope.serializer(), part)

                // Handle Ping (type 6) — auto-respond
                if (envelope.type == 6) return

                // Handle Close (type 7) — trigger reconnect
                if (envelope.type == 7) {
                    _rawMessages.emit("CLOSE: ${envelope.error ?: "Unknown"}")
                    return
                }

                _messages.trySend(envelope)
            } catch (e: Exception) {
                // May be a handshake error or non-JSON message
                _rawMessages.emit(part)
            }
        }
    }

    private fun onDisconnected() {
        connected.set(false)
        currentSession = null

        if (!reconnectAttempt.get()) {
            startReconnect()
        }
    }

    private fun startReconnect() {
        if (reconnectAttempt.compareAndSet(false, true)) {
            _connectionState.value = ConnectionState.Reconnecting
            reconnectJob = scope.launch {
                var delay = 1000L
                val maxDelay = 30000L

                while (isActive && !connected.get()) {
                    try {
                        delay(delay)
                        connect()
                        // Successfully reconnected
                        _connectionState.value = ConnectionState.Connected
                        break
                    } catch (_: Exception) {
                        delay = (delay * 2).coerceAtMost(maxDelay)
                    }
                }
                reconnectAttempt.set(false)
            }
        }
    }
}
