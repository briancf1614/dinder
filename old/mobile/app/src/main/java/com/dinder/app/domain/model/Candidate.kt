package com.dinder.app.domain.model

/**
 * Domain model representing a swipeable candidate in discovery.
 */
data class Candidate(
    val profileId: String,
    val userId: String,
    val displayName: String,
    val bio: String? = null,
    val age: Int,
    val gender: String,
    val latitude: Double? = null,
    val longitude: Double? = null,
    val photoCount: Int,
    val prompts: List<Prompt> = emptyList()
)

data class Prompt(
    val promptId: String,
    val answer: String
)
