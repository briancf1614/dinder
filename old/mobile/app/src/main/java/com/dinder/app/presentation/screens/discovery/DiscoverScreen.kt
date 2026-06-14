package com.dinder.app.presentation.screens.discovery

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import com.dinder.app.domain.model.Candidate
import kotlinx.coroutines.launch
import kotlin.math.roundToInt
import androidx.compose.foundation.gestures.detectHorizontalDragGestures

@Composable
fun DiscoverScreen(
    viewModel: DiscoveryViewModel = hiltViewModel()
) {
    val state by viewModel.state.collectAsState()
    var showMatch by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(Unit) {
        viewModel.matchEvent.collect { matchDisplayName -> showMatch = matchDisplayName }
    }

    Box(Modifier.fillMaxSize().padding(top = 16.dp)) {
        when {
            state.isLoading && state.candidates.isEmpty() -> {
                CircularProgressIndicator(Modifier.align(Alignment.Center))
            }
            state.candidates.isEmpty() -> EmptyStack(state.error) { viewModel.loadCandidates() }
            else -> CardStack(candidates = state.candidates, onSwipe = { id, dir -> viewModel.swipe(id, dir) })
        }

        if (state.swipeLimitReached) {
            SwipeLimitChip(state.resetAt, Modifier.align(Alignment.TopCenter).padding(top = 8.dp))
        }

        showMatch?.let { name ->
            MatchDialog(
                displayName = name,
                onKeepSwiping = { showMatch = null },
                onSendMessage = { showMatch = null }
            )
        }
    }
}

@Composable
private fun EmptyStack(error: String?, onRefresh: () -> Unit) {
    Column(Modifier.fillMaxSize(), horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.Center) {
        Text("No more candidates", style = MaterialTheme.typography.titleLarge)
        if (error != null) Text(error, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
        Spacer(Modifier.height(16.dp))
        Button(onClick = onRefresh) {
            Icon(Icons.Default.Refresh, contentDescription = null, Modifier.size(20.dp))
            Spacer(Modifier.width(8.dp))
            Text("Refresh")
        }
    }
}

@Composable
private fun CardStack(candidates: List<Candidate>, onSwipe: (String, String) -> Unit) {
    Box(Modifier.fillMaxSize().padding(horizontal = 24.dp, vertical = 8.dp), contentAlignment = Alignment.TopCenter) {
        candidates.take(3).reversed().forEachIndexed { revIdx, candidate ->
            val isTop = revIdx == candidates.take(3).reversed().size - 1
            if (isTop) {
                SwipeableCard(candidate, onSwipe)
            } else {
                Box(
                    Modifier
                        .fillMaxWidth(0.88f)
                        .aspectRatio(0.75f)
                        .offset(y = (12 * (3 - revIdx)).dp)
                        .clip(RoundedCornerShape(24.dp))
                        .background(MaterialTheme.colorScheme.surfaceVariant)
                )
            }
        }
    }
}

@Composable
private fun SwipeableCard(candidate: Candidate, onSwiped: (String, String) -> Unit) {
    val scope = rememberCoroutineScope()
    val density = LocalDensity.current
    val thresholdPx = with(density) { 150.dp.toPx() }

    val offsetX = remember { Animatable(0f) }
    val rotation = (offsetX.value / 20f).coerceIn(-15f, 15f)

    val likeOpacity = (offsetX.value / thresholdPx).coerceIn(0f, 1f)
    val passOpacity = (-offsetX.value / thresholdPx).coerceIn(0f, 1f)

    Box(
        Modifier
            .fillMaxWidth(0.9f)
            .aspectRatio(0.75f)
            .offset { IntOffset(offsetX.value.roundToInt(), 0) }
            .graphicsLayer { rotationZ = rotation }
            .pointerInput(candidate.profileId) {
                detectHorizontalDragGestures(
                    onDragEnd = {
                        scope.launch {
                            when {
                                offsetX.value > thresholdPx -> {
                                    offsetX.animateTo(2000f, tween(300))
                                    onSwiped(candidate.profileId, "Right")
                                }
                                offsetX.value < -thresholdPx -> {
                                    offsetX.animateTo(-2000f, tween(300))
                                    onSwiped(candidate.profileId, "Left")
                                }
                                else -> { offsetX.animateTo(0f, tween(300)) }
                            }
                        }
                    },
                    onDragCancel = { scope.launch { offsetX.animateTo(0f, tween(300)) } },
                    onHorizontalDrag = { _, dragAmount ->
                        scope.launch { offsetX.snapTo(offsetX.value + dragAmount) }
                    }
                )
            }
    ) {
        CandidateCard(candidate)

        // Like overlay
        if (likeOpacity > 0f) {
            Text(
                "LIKE", color = Color(0xFF2ECC71).copy(alpha = likeOpacity),
                fontSize = 36.sp, fontWeight = FontWeight.Bold,
                modifier = Modifier.align(Alignment.TopStart).padding(24.dp).graphicsLayer { rotationZ = -15f }
            )
        }
        // Pass overlay
        if (passOpacity > 0f) {
            Text(
                "NOPE", color = Color(0xFFE74C3C).copy(alpha = passOpacity),
                fontSize = 36.sp, fontWeight = FontWeight.Bold,
                modifier = Modifier.align(Alignment.TopEnd).padding(24.dp).graphicsLayer { rotationZ = 15f }
            )
        }
    }
}

@Composable
private fun SwipeLimitChip(resetAt: String?, modifier: Modifier = Modifier) {
    Surface(modifier, shape = RoundedCornerShape(20.dp), color = MaterialTheme.colorScheme.errorContainer) {
        Text(
            text = if (resetAt != null) "Daily limit reached — resets at $resetAt" else "Daily limit reached",
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
            color = MaterialTheme.colorScheme.onErrorContainer,
            style = MaterialTheme.typography.labelMedium
        )
    }
}
