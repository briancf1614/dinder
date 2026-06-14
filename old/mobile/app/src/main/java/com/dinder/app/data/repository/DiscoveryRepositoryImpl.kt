package com.dinder.app.data.repository

import com.dinder.app.data.remote.ApiService
import com.dinder.app.di.TokenRefreshedException
import com.dinder.app.data.remote.dto.SwipeRequest
import com.dinder.app.domain.model.Candidate
import com.dinder.app.domain.model.Match
import com.dinder.app.domain.model.Prompt
import com.dinder.app.domain.repository.DiscoveryRepository
import io.ktor.client.plugins.*
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class DiscoveryRepositoryImpl @Inject constructor(
    private val apiService: ApiService
) : DiscoveryRepository {

    /** Retry a call once on [TokenRefreshedException]. */
    private suspend fun <T> withTokenRetry(block: suspend () -> T): Result<T> =
        try {
            Result.success(block())
        } catch (e: TokenRefreshedException) {
            try { Result.success(block()) }
            catch (e2: Exception) { Result.failure(e2) }
        } catch (e: Exception) {
            Result.failure(e)
        }

    override suspend fun getCandidates(
        latitude: Double, longitude: Double, cursor: String?
    ): Result<Pair<List<Candidate>, String?>> = withTokenRetry {
        val res = apiService.getCandidates(latitude, longitude, cursor)
        val candidates = res.candidates.map { dto ->
            Candidate(
                profileId = dto.profileId,
                userId = dto.userId,
                displayName = dto.displayName,
                bio = dto.bio,
                age = dto.age,
                gender = dto.gender,
                latitude = dto.latitude,
                longitude = dto.longitude,
                photoCount = dto.photoCount,
                prompts = dto.prompts?.map { Prompt(it.promptId, it.answer) } ?: emptyList()
            )
        }
        candidates to res.nextCursor
    }

    override suspend fun swipe(swipedId: String, direction: String): Result<Boolean> = withTokenRetry {
        val res = apiService.swipe(SwipeRequest(swipedId, direction))
        res.isMatch
    }

    override suspend fun getMatches(): Result<List<Match>> = withTokenRetry {
        val res = apiService.getMatches()
        res.matches.map { dto ->
            Match(matchId = dto.matchId, userId = dto.userId,
                displayName = dto.displayName, matchedAt = dto.matchedAt)
        }
    }
}
