package com.dinder.app.presentation.screens.discovery

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.dinder.app.domain.model.Candidate
import com.dinder.app.domain.repository.DiscoveryRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class DiscoveryViewModel @Inject constructor(
    private val discoveryRepository: DiscoveryRepository
) : ViewModel() {

    data class UiState(
        val candidates: List<Candidate> = emptyList(),
        val isLoading: Boolean = false,
        val error: String? = null,
        val remainingSwipes: Int? = null,
        val swipeLimitReached: Boolean = false,
        val resetAt: String? = null
    )

    private val _state = MutableStateFlow(UiState())
    val state: StateFlow<UiState> = _state.asStateFlow()

    private val _matchEvent = MutableSharedFlow<String>() // matched displayName
    val matchEvent: SharedFlow<String> = _matchEvent.asSharedFlow()

    private var latitude: Double = 0.0
    private var longitude: Double = 0.0

    init { loadCandidates() }

    fun setLocation(lat: Double, lng: Double) { latitude = lat; longitude = lng }

    fun loadCandidates() {
        viewModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            discoveryRepository.getCandidates(latitude, longitude)
                .onSuccess { (candidates, _) ->
                    _state.value = _state.value.copy(
                        candidates = candidates, isLoading = false, swipeLimitReached = false
                    )
                }
                .onFailure { e ->
                    val is429 = e.message?.contains("429") == true
                    _state.value = _state.value.copy(
                        isLoading = false,
                        error = if (_state.value.candidates.isEmpty()) e.message else null,
                        swipeLimitReached = is429
                    )
                }
        }
    }

    fun swipe(swipedId: String, direction: String) {
        val current = _state.value.candidates.toMutableList()
        current.removeAll { it.profileId == swipedId }
        _state.value = _state.value.copy(candidates = current)

        viewModelScope.launch {
            discoveryRepository.swipe(swipedId, direction)
                .onSuccess { isMatch ->
                    if (isMatch) {
                        _matchEvent.emit(swipedId) // pass displayName — find from removed candidate
                    }
                }
                .onFailure { e ->
                    if (e.message?.contains("429") == true) {
                        // Rollback card and show limit
                        loadCandidates()
                        _state.value = _state.value.copy(swipeLimitReached = true)
                    } else {
                        // For other errors, just reload stack
                        if (_state.value.candidates.isEmpty()) loadCandidates()
                    }
                }
        }
    }
}
