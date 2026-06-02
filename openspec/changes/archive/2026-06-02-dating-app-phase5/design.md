# Design: Dating App Phase 5 — Kotlin Mobile App

## Technical Approach

Single-module Compose project consuming the existing backend with ZERO API changes. MVVM + Clean Architecture (data/domain/presentation packages). Ktor HTTP for REST, Ktor WebSocket implementing SignalR JSON hub protocol manually. No backend changes — this is purely additive at `src/Dinder.Mobile/`.

## Architecture Decisions

### 1. Single Module vs Gradle Multi-Module

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Single `:app` + package layers | Fast build, low config overhead, enforced by convention | **Chosen** |
| Multi-module (`:data`, `:domain`, `:presentation`) | Compile-time boundaries, but ~600 lines total across 3 PRs | Rejected |

**Rationale**: Multi-module overhead unjustified at this scale. Package separation (`com.dinder.mobile.data`, `.domain`, `.presentation`) enforces Clean Architecture boundaries. Modularize later if app grows.

### 2. SignalR via Ktor WebSocket

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Manual SignalR JSON protocol over Ktor `webSocket()` | No external dep, full coroutine control, needs handshake impl | **Chosen** |
| OkHttp-based unofficial SignalR client | Extra 200KB dep, Kotlin-coroutine impedance mismatch | Rejected |

**Rationale**: SignalR JSON hub protocol is trivial: handshake (`{"protocol":"json","version":1}\x1e`), invocations as `{"type":1,"target":"Method","arguments":[...]}\x1e` with `\x1e` record delim. Ktor WS already handles binary framing, auto-reconnect with exponential backoff (1s, 2s, 4s, 8s, max 30s) mirroring Angular PWA pattern.

### 3. Token Storage

**Choice**: `EncryptedSharedPreferences` (AndroidX Security, AES-256 master key).  
**Rejected**: DataStore with manual encryption (unnecessary).  
**Rationale**: Standard for secure token persistence. Non-sensitive prefs (theme, onboarding flags) use Jetpack DataStore.

## Data Flow

```
Compose UI ──▶ ViewModel (StateFlow) ──▶ UseCase ──▶ Repository (domain interface)
                                                          │
                                         ┌────────────────┼────────────────┐
                                         ▼                ▼                ▼
                                    Ktor HTTP       Ktor WS +        EncryptedSP /
                                    (ApiService)    SignalRClient    DataStore
```

All API calls flow through `AuthInterceptor` (Ktor plugin): on 401 → `POST /api/v1/identity/refresh` → retry. Refresh failure clears tokens and emits `SessionExpired` to navigation.

## Navigation

```
NavHost(root)
├── authGraph (no bottom bar)
│   ├── LoginScreen → RegisterScreen
│   └── deepLink: notification → auth check → redirect
└── mainGraph (BottomNav: Discover | Matches | Profile)
    ├── DiscoverScreen → MatchDialog overlay
    ├── MatchesScreen → ChatScreen (nested)
    └── ProfileScreen → settings, delete account
```

## SignalR Hub Contracts

Hub URLs: `/hubs/chat`, `/hubs/notifications` (JWT in query string `?access_token=...`)

| Direction | Method | Hub |
|-----------|--------|-----|
| Client→Server | `JoinConversation(guid)`, `SendMessage(guid, string)`, `TypingIndicator(guid, bool)`, `MarkRead(guid)`, `LeaveConversation(guid)` | ChatHub |
| Server→Client | `ReceiveMessage({messageId,...})`, `TypingUpdate({userId, isTyping})`, `MessageRead({...})` | ChatHub |
| Auto-on-connect | User added to `user_{userId}` group | NotificationHub |
| Server→Client | `NewNotification({...})`, `BadgeUpdate({unreadCount})` | NotificationHub |

## File Structure (key files)

```
src/Dinder.Mobile/app/src/main/java/com/dinder/mobile/
├── DinderApplication.kt                  @HiltAndroidApp
├── MainActivity.kt                       setContent { DinderApp() }
├── data/remote/
│   ├── ApiService.kt                     Ktor HTTP: all REST endpoints
│   ├── SignalRClient.kt                  Ktor WS → SignalR protocol (handshake, invoke, receive)
│   ├── SignalRMessage.kt                 @Serializable message types
│   ├── AuthInterceptor.kt                Ktor plugin: JWT header, 401→refresh→retry
│   └── dto/{Auth,Discovery,Chat,Notification}Dtos.kt
├── data/local/{TokenStorage,PreferencesStore}.kt
├── data/repository/{Auth,Discovery,Chat,Notification}RepositoryImpl.kt
├── domain/model/{User,Candidate,Conversation,Message,Notification}.kt
├── domain/usecase/{Login,Register,RefreshToken,GetCandidates,Swipe,...}.kt
├── domain/repository/{Auth,Discovery,Chat,Notification}Repository.kt  ← interfaces
├── presentation/
│   ├── navigation/{DinderNavHost,AuthNavGraph,MainNavGraph}.kt
│   ├── theme/{Color,Type,Theme}.kt
│   └── screens/{auth,discovery,chat,notifications,profile}/
└── di/AppModule.kt                       Hilt @Module: ApiService, repos, DataStore, SignalRClient
```

## Theme

Material 3 with Dinder brand: Primary `#FF6B6B` (coral), Secondary `#4ECDC4` (teal), Surface containers for cards. Typography: default M3 scale with `DisplaySmall` for branding. Dark theme: dark surface + elevated cards.

## Open Questions

None — all decisions resolved from existing backend contracts and Angular SignalR patterns.
