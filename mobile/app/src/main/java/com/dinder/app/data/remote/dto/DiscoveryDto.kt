package com.dinder.app.data.remote.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

// ── Request DTOs ──────────────────────────────────────────────────────

@Serializable
data class SwipeRequest(
    @SerialName("swipedId")
    val swipedId: String,
    val direction: String // "Left" | "Right"
)

// ── Response DTOs ─────────────────────────────────────────────────────

@Serializable
data class CandidatesResponse(
    val candidates: List<CandidateDto>,
    @SerialName("nextCursor")
    val nextCursor: String? = null
)

@Serializable
data class CandidateDto(
    @SerialName("profileId")
    val profileId: String,
    @SerialName("userId")
    val userId: String,
    @SerialName("displayName")
    val displayName: String,
    val bio: String? = null,
    val age: Int,
    val gender: String,
    val latitude: Double? = null,
    val longitude: Double? = null,
    @SerialName("photoCount")
    val photoCount: Int,
    val prompts: List<CandidatePromptDto>? = null
)

@Serializable
data class CandidatePromptDto(
    @SerialName("promptId")
    val promptId: String,
    val answer: String
)

@Serializable
data class SwipeResponse(
    @SerialName("isMatch")
    val isMatch: Boolean,
    @SerialName("matchId")
    val matchId: String? = null
)

@Serializable
data class SwipeLimitError(
    val error: String,
    @SerialName("resetAt")
    val resetAt: String? = null,
    @SerialName("upgrade_url")
    val upgradeUrl: String? = null,
    @SerialName("upgrade_tier")
    val upgradeTier: String? = null
)

@Serializable
data class MatchesResponse(
    val matches: List<MatchDto>,
    @SerialName("nextCursor")
    val nextCursor: String? = null
)

@Serializable
data class MatchDto(
    @SerialName("matchId")
    val matchId: String,
    @SerialName("userId")
    val userId: String,
    @SerialName("displayName")
    val displayName: String,
    @SerialName("matchedAt")
    val matchedAt: String
)
