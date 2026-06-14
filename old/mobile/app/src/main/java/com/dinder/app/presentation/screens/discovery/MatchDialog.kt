package com.dinder.app.presentation.screens.discovery

import androidx.compose.animation.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.dinder.app.presentation.theme.Coral
import com.dinder.app.presentation.theme.Teal
import kotlinx.coroutines.delay

@Composable
fun MatchDialog(
    displayName: String,
    onKeepSwiping: () -> Unit,
    onSendMessage: () -> Unit
) {
    var visible by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        visible = true
        delay(600) // Brief confetti-like entrance delay
    }

    AnimatedVisibility(visible, enter = fadeIn() + scaleIn(), exit = fadeOut() + scaleOut()) {
        Dialog(
            onDismissRequest = onKeepSwiping,
            properties = DialogProperties(usePlatformDefaultWidth = false)
        ) {
            Surface(
                Modifier.fillMaxSize(),
                color = Color(0xCC1A1A2E) // Semi-transparent dark overlay
            ) {
                Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(32.dp)) {
                        // Photo placeholders (two circles)
                        Row(horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically) {
                            Box(Modifier.size(100.dp).clip(RoundedCornerShape(50)).background(Coral))
                            Spacer(Modifier.width((-16).dp))
                            Box(Modifier.size(100.dp).clip(RoundedCornerShape(50)).background(Teal))
                        }
                        Spacer(Modifier.height(24.dp))

                        Text(
                            "It's a Match!",
                            fontSize = 32.sp, fontWeight = FontWeight.Bold,
                            color = Color.White, textAlign = TextAlign.Center
                        )
                        Spacer(Modifier.height(8.dp))
                        Text(
                            "You and $displayName liked each other",
                            color = Color.White.copy(alpha = 0.8f),
                            style = MaterialTheme.typography.bodyLarge,
                            textAlign = TextAlign.Center
                        )
                        Spacer(Modifier.height(40.dp))

                        Button(
                            onClick = onSendMessage,
                            modifier = Modifier.fillMaxWidth(0.7f).height(50.dp),
                            colors = ButtonDefaults.buttonColors(containerColor = Coral),
                            shape = RoundedCornerShape(24.dp)
                        ) { Text("Send Message") }

                        Spacer(Modifier.height(12.dp))

                        OutlinedButton(
                            onClick = onKeepSwiping,
                            modifier = Modifier.fillMaxWidth(0.7f).height(50.dp),
                            shape = RoundedCornerShape(24.dp),
                            colors = ButtonDefaults.outlinedButtonColors(contentColor = Color.White)
                        ) { Text("Keep Swiping") }
                    }
                }
            }
        }
    }
}
