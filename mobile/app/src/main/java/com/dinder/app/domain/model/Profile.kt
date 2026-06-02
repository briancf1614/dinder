package com.dinder.app.domain.model

/**
 * Domain model representing a user profile (own or match).
 */
data class Profile(
    val userId: String,
    val displayName: String,
    val bio: String? = null,
    val gender: String,
    val age: Int,
    val photoCount: Int,
    val latitude: Double? = null,
    val longitude: Double? = null
)
