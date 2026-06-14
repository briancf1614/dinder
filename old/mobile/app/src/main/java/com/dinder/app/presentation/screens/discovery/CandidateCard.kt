package com.dinder.app.presentation.screens.discovery

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import coil.compose.AsyncImage
import com.dinder.app.domain.model.Candidate

@Composable
fun CandidateCard(candidate: Candidate) {
    Box(
        Modifier
            .fillMaxSize()
            .clip(RoundedCornerShape(24.dp))
            .background(MaterialTheme.colorScheme.surfaceVariant)
    ) {
        // Photo placeholder — use first photo index
        AsyncImage(
            model = null, // TODO: wire actual photo URLs when backend serves them
            contentDescription = candidate.displayName,
            contentScale = ContentScale.Crop,
            modifier = Modifier.fillMaxSize()
        )
        // Fallback gradient overlay
        Box(
            Modifier
                .fillMaxSize()
                .background(
                    brush = androidx.compose.ui.graphics.Brush.verticalGradient(
                        colors = listOf(Color.Transparent, Color.Black.copy(alpha = 0.7f)),
                        startY = 200f, endY = Float.POSITIVE_INFINITY
                    )
                )
        )

        // Info overlay at bottom
        Column(
            Modifier.align(Alignment.BottomStart).padding(20.dp)
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(candidate.displayName, style = MaterialTheme.typography.titleLarge, color = Color.White, fontWeight = FontWeight.Bold)
                Spacer(Modifier.width(8.dp))
                Text("${candidate.age}", style = MaterialTheme.typography.titleMedium, color = Color.White.copy(alpha = 0.9f))
            }
            candidate.prompts.firstOrNull()?.let { prompt ->
                Spacer(Modifier.height(4.dp))
                Text(
                    "\"${prompt.answer}\"",
                    style = MaterialTheme.typography.bodyMedium,
                    color = Color.White.copy(alpha = 0.85f),
                    maxLines = 2
                )
            }
        }
    }
}
