package com.dinder.app.domain.model

/**
 * Domain model representing the authenticated user.
 */
data class User(
    val id: String,
    val email: String,
    val tier: String = "Free",
    val birthday: String? = null,
    val createdAt: String? = null
)
