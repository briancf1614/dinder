package com.dinder.app.data.remote

import kotlinx.serialization.*
import kotlinx.serialization.json.*

/**
 * SignalR JSON Hub Protocol message types.
 *
 * Record delimiter: `\x1E` (ASCII 0x1E / 30).
 * Every message ends with this delimiter, used for framing over WebSocket/text stream.
 */
object SignalRMessage {
    const val RECORD_DELIMITER: Char = '\u001E' // 0x1E

    @Serializable
    data class HandshakeRequest(
        val protocol: String = "json",
        val version: Int = 1
    )

    /**
     * Generic SignalR message envelope.
     * @param type 1=Invocation, 2=StreamItem, 3=Completion, 6=Ping, 7=Close
     */
    @Serializable
    data class Envelope(
        val type: Int? = null,
        val invocationId: String? = null,
        val target: String? = null,
        val arguments: JsonArray? = null,
        val item: JsonElement? = null,
        val result: JsonElement? = null,
        val error: String? = null,
        val allowReconnect: Boolean? = null
    )

    /** Server-pushed notification from NotificationHub. */
    @Serializable
    data class NewNotificationPayload(
        val notificationId: String? = null,
        val type: String? = null,
        val title: String? = null,
        val body: String? = null,
        val deepLinkPayload: String? = null,
        val isRead: Boolean? = null,
        val createdAt: String? = null
    )

    /** Badge count update from NotificationHub. */
    @Serializable
    data class BadgeUpdatePayload(
        val unreadCount: Int
    )

    /** Chat message received from ChatHub. */
    @Serializable
    data class ReceiveMessagePayload(
        val messageId: String,
        val conversationId: String,
        val senderId: String,
        val content: String,
        val sentAt: String
    )

    /** Typing indicator from ChatHub. */
    @Serializable
    data class TypingUpdatePayload(
        val userId: String,
        val conversationId: String,
        val isTyping: Boolean
    )

    /** Message read receipt from ChatHub. */
    @Serializable
    data class MessageReadPayload(
        val conversationId: String,
        val readByUserId: String
    )

    /** Force disconnect (e.g., account banned). */
    @Serializable
    data class ForceDisconnectPayload(
        val reason: String,
        val message: String
    )

    /** Error from hub. */
    @Serializable
    data class HubError(
        val error: String? = null
    )
}
