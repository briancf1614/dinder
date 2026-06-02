## Exploration: Dating App Phase 5 — Next Features

### Current State

4 SDD phases complete. **223 tests passing** (0 failures, 0 warnings). .NET 10 Clean Architecture (CQRS + MediatR), Angular 20 standalone components, PostgreSQL+PostGIS, SignalR real-time.

**Real-time infrastructure** (mature, production-ready):
- `ChatHub` — 5 methods: `SendMessage`, `TypingIndicator`, `MarkRead`, `JoinConversation`, `LeaveConversation`. Match-gated via `IsParticipantAsync()`. Group routing: `conversation_{id}`.
- `NotificationHub` — per-user groups (`user_{id}`), `SendNotificationAsync` static helper, badge updates. Used by `AchievementPushService`, `UserBannedSignalRHandler`.
- JWT auth on SignalR query string (`access_token`), automatic reconnect on Angular side (`@microsoft/signalr`).
- Angular `SignalRService`: dual-hub connection with `withAutomaticReconnect()`.
- Domain event pipeline: MediatR `INotificationHandler<T>` — all existing events (login, swipe, match, message, achievement) already wired to SignalR pushes.

**Key correction from Phase 4 audit**: The Phase 4 exploration (2026-06-02) reported Kotlin/Gradle/Android SDK as NOT INSTALLED. That audit was wrong — it only checked standalone CLI tools and `PATH`, not Android Studio's bundled toolchain. **The full Kotlin toolchain IS installed**:

| Component | Status | Version |
|-----------|--------|---------|
| Android Studio | INSTALLED | `C:\Program Files\Android\Android Studio` |
| Kotlin compiler (kotlinc) | INSTALLED | 2.2.20 |
| Kotlin script runner (kotlin) | INSTALLED | 2.2.20-release-333 |
| Java JDK (JetBrains Runtime) | INSTALLED | 21.0.10 |
| Android SDK platforms | INSTALLED | 35, 36.1 |
| Android SDK build-tools | INSTALLED | 34.0.0, 35.0.0, 36.0.0, 36.1.0 |
| Android SDK system-images | INSTALLED | Available for API 35, 36 |
| `ANDROID_HOME` env var | NOT SET | Needs `$env:LOCALAPPDATA\Android\Sdk` |
| `JAVA_HOME` env var | NOT SET | Needs `C:\Program Files\Android\Android Studio\jbr` |
| Gradle (standalone) | NOT INSTALLED | Not needed — Android Studio projects use Gradle wrapper |
| `cmdline-tools` | NOT INSTALLED | Minor — only needed for SDK updates from CLI |

**Verdict**: Kotlin Mobile toolchain is 90% ready. Only 2 environment variables need setting. No toolchain install phase needed — can scaffold a project and start coding immediately.

### Affected Areas

#### For Kotlin Mobile (if chosen)
- New project dir: `src/Dinder.Mobile/` — Jetpack Compose + Material 3
- `src/Dinder.Api/` — Existing REST + SignalR endpoints consumed as-is (no backend changes)
- `openspec/config.yaml` — Update mobile `status` from "not installed" to current versions
- No existing code modified — entirely additive

#### For Video Chat (if chosen)
- `src/Dinder.Infrastructure/SignalR/` — New `VideoCallHub` or extend `ChatHub` with signaling methods
- `src/Dinder.Api/Program.cs` — Map new hub endpoint (`/hubs/videocall`)
- `src/app/src/app/core/signalr/signalr.service.ts` — Add VideoCallHub connection
- `src/app/src/app/features/chat/` — New video call UI components, WebRTC peer connections
- `src/Dinder.Domain/` — New `VideoCall` entity, `VideoCallSession` value object
- New infrastructure: TURN/STUN server (managed service or self-hosted coturn)
- `src/Dinder.Application/Chat/Commands/` — `InitiateVideoCallCommand`, `AcceptVideoCallCommand`, `EndVideoCallCommand`

#### For Virtual Speed Dating (if chosen)
- `src/Dinder.Domain/` — New entities: `SpeedDatingEvent`, `EventRound`, `EventParticipant`, `EventHost`
- `src/Dinder.Infrastructure/SignalR/` — New `SpeedDatingHub` with room management
- New infrastructure: Scheduled job runner (Hangfire/Quartz.NET)
- `src/Dinder.Application/` — Event scheduling, round rotation, pairing algorithm
- Frontend: Event lobby, countdown timers, round transition UI

### Approaches

#### 1. Kotlin Mobile (Native Android)

- Pros:
  - **VERY HIGH impact**: Opens the entire mobile user acquisition channel (150M+ US smartphone users)
  - **LOW risk**: Just consumes existing REST + SignalR API — no backend changes, no breaking changes
  - **Toolchain is READY**: Kotlin 2.2.20, Java 21, Android SDK 36.1 all installed (contrary to Phase 4 audit)
  - **0 infrastructure risk**: No new services, no new DB schema, no operational changes
  - **Product differentiator**: Native Android app is aspirational for a dating platform
  - **Iterative delivery**: Can deliver login → discovery → chat in 3 incremental PRs
  - **No legal/privacy concerns**: Mobile app follows same backend data governance
- Cons:
  - High effort (12-15 tasks for toolchain setup + first 3 screens)
  - Need to set `ANDROID_HOME` and `JAVA_HOME` env vars (2 trivial tasks)
  - Shared API client (Ktor/Retrofit) + SignalR client (ktor-client-websockets) need to be built
  - Testing on Android emulator or physical device required
  - Angular PWA exists as a fallback web mobile experience (reduces urgency perception)
- Effort: **High** (12-15 tasks, ~1000-1500 changed lines, 3 chained PRs)

#### 2. Video Chat (WebRTC + SignalR signaling)

- Pros:
  - **VERY HIGH impact**: Premium differentiator — keeps users in-app, no phone number exchange
  - Leverages mature SignalR infrastructure (JWT auth, groups, reconnect, `IsParticipantAsync`)
  - Match-gated access maps directly to existing `ChatHub.JoinConversation` pattern
  - SignalR is the IDEAL signaling channel for WebRTC (bidirectional, low-latency)
  - Angular browser WebRTC support is mature (native `RTCPeerConnection`, `MediaStream` APIs)
  - Can be monetized (Premium-only video calls, or time-limited for free tier)
  - Existing `Conversation` entity can be extended with `HasActiveVideoCall` flag
- Cons:
  - **TURN/STUN server needed** — new infrastructure (managed: Twilio ~$0.004/min or self-hosted: coturn on a $5 VPS)
  - WebRTC peer connection state machine is complex (offer/answer, ICE candidates, renegotiation, trickle ICE)
  - Legal/privacy: GDPR right-to-access for video content, potential recording concerns (mitigation: no server-side recording, client-only)
  - Testing WebRTC is difficult — needs real browser environments or WebRTC mocks
  - Frontend complexity: camera permissions, video UI, mute/camera toggle, call timer, picture-in-picture
  - Media server for group calls (if extending beyond 1-on-1) would multiply complexity
- Effort: **High** (8-12 tasks, ~800-1000 changed lines, 2-3 chained PRs)

#### 3. Virtual Speed Dating

- Pros:
  - SignalR groups already handle multi-user rooms (just need event-scoped groups)
  - Unique differentiator vs Tinder/Bumble (neither has live speed dating)
  - Could be monetized (premium events, priority access, "skip the line")
  - Chat infrastructure already exists for timed conversations
- Cons:
  - **Critical mass problem is fundamental**: Empty events kill user trust in the feature
  - Needs scheduled job infrastructure (Hangfire, Quartz.NET, or Azure Functions) — none exists
  - Complex state machine: scheduled → active → round → break → round → finished
  - Timezone scheduling is notoriously bug-prone (DST transitions, international events)
  - Pairing algorithm for N participants with gender preferences is non-trivial (stable matching variant)
  - Frontend complexity: event lobby, countdown timers, round transitions, "next match" animations
  - Testing requires simulating multi-user scenarios (SignalR test infrastructure needed)
- Effort: **High** (10-15 tasks, ~1200-1800 changed lines, 3-4 chained PRs)
- Risk: **HIGH**

### Recommendation

**Kotlin Mobile is the clear winner** — highest impact with lowest risk. The Phase 4 exploration deferred it because the toolchain audit was wrong (only checked `PATH`, missed Android Studio's bundled Kotlin + SDK). Now that we've verified the full toolchain is installed, Kotlin Mobile is the #1 priority for Phase 5.

**Why Kotlin Mobile over Video Chat**:
1. **Risk profile**: Kotlin Mobile adds NO backend complexity — it's a pure API consumer. Video Chat adds TURN infrastructure, WebRTC state machine, and legal/privacy surface.
2. **Impact**: Both are "Very High," but Kotlin Mobile opens an ENTIRELY NEW acquisition channel (Google Play Store). Video Chat enhances existing web users.
3. **Momentum**: Kotlin Mobile was deferred in Phase 4 due to a false-negative toolchain audit. The discovery that the toolchain IS ready means we can correct course and ship it NOW.
4. **Delivery**: Kotlin Mobile can be delivered as 3 chained PRs (scaffold → auth → discovery+chat). Video Chat's TURN infra creates an external dependency.
5. **Synergy with Phase 4**: Phase 4 added Gamification (streaks, achievements, rewards). All of this is available to the mobile app via the existing API — the mobile app gets Phase 4 features "for free."

**Suggested Phase 5 scope**:
| Priority | Deliverable | Tasks Est. | Rationale |
|----------|-------------|-----------|-----------|
| P0 | Kotlin Mobile — Toolchain setup | 2 | Set ANDROID_HOME, JAVA_HOME; verify Gradle wrapper |
| P0 | Kotlin Mobile — Scaffold + API client | 3 | Jetpack Compose project, Ktor HTTP client, SignalR WebSocket client |
| P1 | Kotlin Mobile — Auth & Discovery | 5 | Login/Register, swipe cards, profile view |
| P2 | Kotlin Mobile — Chat & Notifications | 4 | Real-time chat via SignalR, notification badge |

**Deferred to Phase 6**: Video Chat (high impact, but TURN infra + legal review needed first)
**Deferred to Phase 7+**: Virtual Speed Dating (critical mass problem is unresolved)

### Risks

- **Kotlin Mobile delivery scope creep**: 14 tasks is a large phase. Mitigation: deliver in 3 chained PRs with clear milestones. PR1 (toolchain+scaffold) alone proves viability.
- **Android emulator performance on Windows**: May be slow without HAXM/WHPX. Mitigation: physical device testing as fallback.
- **SignalR client on Android**: Ktor WebSocket client must handle JWT auth + reconnect. The backend SignalR already supports JWT query string auth — this works for any WebSocket client.
- **API surface drift**: The REST API has 10 controllers with ~25 endpoints. The mobile app only needs 6-8 endpoints initially. Mitigation: document the exact API contract used by mobile in Phase 5, so backend changes are gated on mobile impact.
- **Video Chat still valuable**: Deferring to Phase 6 is fine — the SignalR infrastructure is ready, and TURN services exist. The delay lets us research legal/privacy requirements properly.

### Other High-Value Features (Not Yet Considered)

- **Social Auth (Google/Apple/Facebook login)**: Increases signup conversion by 30-40%. ASP.NET Identity supports external providers natively. 4-6 tasks, LOW risk.
- **Voice Messages in Chat**: SignalR binary data + MediaRecorder API. Builds on existing `ChatHub.SendMessage` pattern. 4-6 tasks, LOW-MEDIUM risk.
- **"Super Like" / Premium Visibility**: Monetization feature. Extends `SwipeCommand` with premium flag. Existing Stripe + entitlement infrastructure. 3-4 tasks, LOW risk.
- **Location-Based Discovery Improvements**: PostGIS already installed. "Show me users near [poi]" or "within neighborhood" queries. 3-5 tasks, LOW risk (extends existing ST_DWithin queries).

### Ready for Proposal

**Yes**. Kotlin Mobile is the recommended Phase 5 focus. Video Chat is deferred to Phase 6 for TURN infra + legal review. Speed Dating remains deferred indefinitely.

Proceed to `sdd-propose` with:
- Change name: `dating-app-phase5`
- Scope: Kotlin Mobile (toolchain setup → scaffold → API client → auth → discovery → chat)
- Deferred: Video Chat (Phase 6), Virtual Speed Dating (indefinite), Social Auth, Voice Messages
- Chained PR strategy: 3 PRs (scaffold → auth+discovery → chat+notifications)
