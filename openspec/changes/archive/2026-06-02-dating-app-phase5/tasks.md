# Tasks: Dating App Phase 5 — Kotlin Mobile App

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~200 (PR1) + ~350 (PR2) + ~350 (PR3) |
| 400-line budget risk | Medium |
| Chained PRs recommended | Yes |
| Suggested split | PR1 → PR2 → PR3 (feature-branch-chain) |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Scaffold + API client + SignalR protocol | PR 1 | Base: feature/dating-app-phase5 |
| 2 | Auth screens + Discovery swipe UI | PR 2 | Base: PR 1 branch |
| 3 | Chat + Notifications + final integration | PR 3 | Base: PR 2 branch |

## PR 1: Scaffold + API Client (~200 lines)

### Phase 1: Project Scaffold
- [x] 1.1 Set ANDROID_HOME, JAVA_HOME env vars; verify Gradle wrapper
- [x] 1.2 Scaffold `src/Dinder.Mobile/` Gradle project: Compose BOM, Material 3, Ktor 3.1, Hilt 2.56, Security-Crypto 1.1.0, Firebase BOM
- [x] 1.3 Create `DinderApplication.kt` (@HiltAndroidApp) and `MainActivity.kt` (setContent { DinderApp() })
- [x] 1.4 Create theme files (`Color.kt`, `Type.kt`, `Theme.kt`) — M3 with brand coral/teal, dark theme support
- [x] 1.5 Create navigation skeleton: `DinderNavHost.kt`, `AuthNavGraph.kt` stub, `MainNavGraph.kt` stub

### Phase 2: Data Layer Foundation
- [x] 2.1 Create `data/remote/dto/AuthDto.kt`, `DiscoveryDto.kt`, `ChatDto.kt`, `NotificationDto.kt` — @Serializable matching all API contracts
- [x] 2.2 Create `data/remote/ApiService.kt` — Ktor HttpClient with ContentNegotiation(Json), HttpLogging, base URL config
- [x] 2.3 Create `data/remote/AuthInterceptor.kt` — Ktor plugin: attach JWT header, 401→refresh→retry, emit SessionExpired on failure
- [x] 2.4 Create `data/remote/SignalRMessage.kt` — @Serializable handshake, invocation, receive message types
- [x] 2.5 Create `data/remote/SignalRClient.kt` — Ktor webSocket(): handshake (`{"protocol":"json","version":1}\x1e`), invoke(), receive Flow, auto-reconnect (1s-30s exponential backoff)
- [x] 2.6 Create `data/local/TokenStorage.kt` — EncryptedSharedPreferences: save/get/clear access+refresh tokens
- [x] 2.7 Create `data/local/PreferencesStore.kt` — DataStore: theme preference, onboarding flags

### Phase 3: Domain + DI Wiring
- [x] 3.1 Create `domain/model/` — User, Candidate, Conversation, Message, Notification data classes
- [x] 3.2 Create `domain/repository/` — AuthRepository, DiscoveryRepository, ChatRepository, NotificationRepository interfaces
- [x] 3.3 Create `di/AppModule.kt` — Hilt @Module providing ApiService, TokenStorage, DataStore, SignalRClient, repos

## PR 2: Auth + Discovery (~350 lines)

### Phase 4: Auth Data + Screens
- [x] 4.1 Create `data/repository/AuthRepositoryImpl.kt` — login, loginExternal(Google), register, refresh, deleteAccount
- [x] 4.2 Create `LoginUseCase`, `RegisterUseCase`, `RefreshTokenUseCase` — password validation (8+ chars, 1 upper, 1 digit), token lifecycle
- [x] 4.3 Implement cold-start session restore: check stored tokens → valid skip login; expired → auto-refresh before UI
- [x] 4.4 Create `LoginScreen.kt` — email/password fields, Google Sign-In button, inline 401 errors
- [x] 4.5 Create `RegisterScreen.kt` — email, password complexity check, M3 DatePicker birthday, age gate 422 error
- [x] 4.6 Wire `AuthNavGraph` — Login ↔ Register navigation; deep link notification → auth check → redirect

### Phase 5: Discovery Data + UI
- [x] 5.1 Create `data/repository/DiscoveryRepositoryImpl.kt` — getCandidates(lat,lng), swipe(swipedId,direction), handle 429 with resetAt
- [x] 5.2 Create `GetCandidatesUseCase`, `SwipeUseCase` — fetch stack, optimistic swipe removal, 429 card rollback
- [x] 5.3 Add GPS permission flow: request location → populate lat/lng on candidate fetch
- [x] 5.4 Create `DiscoverScreen.kt` — card stack with Compose DragGesture, swipe threshold animation, empty stack placeholder
- [x] 5.5 Create `CandidateCard.kt` — primary photo, name, age, prompt, like/pass overlay indicators
- [x] 5.6 Create `MatchDialog.kt` — full-screen "It's a Match!" overlay, both user photos, "Continue" dismiss
- [x] 5.7 Add daily swipe limit display — chip showing remaining count, progress bar
- [x] 5.8 Create `ProfileScreen.kt` — user info, delete account with confirmation dialog, token clear on success
- [x] 5.9 Wire `MainNavGraph` — BottomNav (Discover, Matches, Profile); DiscoverScreen active; MatchesScreen placeholder

## PR 3: Chat + Notifications + Integration (~350 lines)

### Phase 6: Chat Data + SignalR Wiring
- [x] 6.1 Create `data/repository/ChatRepositoryImpl.kt` — getConversations(cursor), getMessages(convId,cursor), sendMessage, unmatch
- [x] 6.2 Create `GetConversationsUseCase`, `SendMessageUseCase`, `UnmatchUseCase` → ChatViewModel (consistent with PR 2 ViewModel pattern)
- [x] 6.3 Wire ChatHub in `SignalRClient`: JoinConversation, LeaveConversation, SendMessage, MarkRead, TypingIndicator hooks
- [x] 6.4 Add app lifecycle: foreground → reconnect WS + re-join active conversation; background → LeaveConversation + disconnect

### Phase 7: Chat UI
- [x] 7.1 Create `MatchesScreen.kt` — conversation list, unread badges, cursor infinite scroll
- [x] 7.2 Create `ChatScreen.kt` — message list (self right-aligned, match left), input field, load-more at top
- [x] 7.3 Add typing indicator: 3s debounce → TypingIndicator; display "{name} is typing…" below last message
- [x] 7.4 Add icebreaker banner from conversation metadata; unmatch flow with confirmation dialog

### Phase 8: Notifications Data + UI
- [x] 8.1 Create `data/repository/NotificationRepositoryImpl.kt` — getNotifications(cursor), registerToken, optOut, markRead
- [x] 8.2 Create `RegisterFcmTokenUseCase`, `GetNotificationsUseCase`, `MarkReadUseCase` → NotificationViewModel
- [x] 8.3 Wire NotificationHub in `SignalRClient`: auto-connect, handle NewNotification (insert top), BadgeUpdate (set count)
- [x] 8.4 Create `NotificationCenterScreen.kt` — list with infinite scroll, tap deep-link, mark all read action
- [x] 8.5 Add badge: bell icon from BadgeUpdate SignalR; cold start fallback from GET /notifications unread count
- [x] 8.6 Add per-type opt-out toggles (match, message, promotional) → PUT /notifications/opt-out
- [x] 8.7 Add Firebase/FCM: google-services.json, token registration on login + token refresh, manifest entry

### Phase 9: Final Integration
- [x] 9.1 Wire MatchesScreen → ChatScreen nav; NotificationCenter from profile/bell icon
- [x] 9.2 Wire deep links: notification tap → ChatScreen(conversationId) or match display
- [x] 9.3 Update `openspec/config.yaml` mobile toolchain status to verified
- [x] 9.4 Verify: `.\gradlew assembleDebug` 0 errors; BUILD SUCCESSFUL
