package com.dinder.app.domain.model

/**
 * Domain model representing a matched conversation.
 */
data class Match(
    val matchId: String,
    val userId: String,
    val displayName: String,
    val matchedAt: String
)
