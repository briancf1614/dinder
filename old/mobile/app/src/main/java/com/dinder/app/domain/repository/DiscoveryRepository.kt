package com.dinder.app.domain.repository

import com.dinder.app.domain.model.Candidate
import com.dinder.app.domain.model.Match

/**
 * Repository for discovery / swipe operations.
 */
interface DiscoveryRepository {
    suspend fun getCandidates(
        latitude: Double = 0.0,
        longitude: Double = 0.0,
        cursor: String? = null
    ): Result<Pair<List<Candidate>, String?>> // candidates + nextCursor

    suspend fun swipe(swipedId: String, direction: String): Result<Boolean> // isMatch
    suspend fun getMatches(): Result<List<Match>>
}
