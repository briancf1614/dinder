## Exploration: Dating App Phase 4 — Feature Candidates

### Current State

The Dinder dating app has completed 3 SDD phases:
- **Phase 1** (MVP): Identity, profiles, discovery, chat, media, notifications (7 specs)
- **Phase 2** (Monetization): Stripe subscriptions, entitlements (4 specs delta)
- **Phase 3** (Social/Safety/Analytics): profile prompts, icebreakers, AI photo moderation, analytics metrics, admin dashboard (5 new specs + 3 deltas)

**Metrics**: 15 specs, 11 bounded contexts, 184 tests (173 unit + 11 integration) all passing, 0 build warnings, 0 test failures.

**Architecture**: .NET 10 Clean Architecture (CQRS + MediatR), PostgreSQL+PostGIS, SignalR real-time (ChatHub + NotificationHub), Angular 20 PWA standalone components. Infrastructure: 10 DbContexts (per-bounded-context), Stripe payment pipeline, Azure AI Vision moderation, JWT auth.

**Real-time infrastructure**: SignalR is production-ready with JWT auth on query string, automatic reconnect on the Angular side (`@microsoft/signalr`), group-based routing (`conversation_{id}` for chat, `user_{id}` for notifications), and existing domain event pipeline (MediatR `INotificationHandler<T>`).

**Known debt**: One WARNING from Phase 3 verify — missing `GET /api/v1/conversations` endpoint. `ChatController` only has `GET /conversations/{id}/messages` and `POST /conversations/{id}/unmatch`. `IChatRepository` has `GetConversationAsync()` but it is not exposed via REST. The Angular `ConversationHeaderComponent` has `@Input() icebreakerQuestion` but the parent cannot wire it to backend data. Icebreaker data is stored correctly on the `Conversation` entity (`IcebreakerQuestion`, `IcebreakerCategory` columns) — only the API bridge is missing.

### Affected Areas

| Area | Current State | Phase 4 Relevance |
|------|--------------|-------------------|
| `src/Dinder.Api/Controllers/ChatController.cs` | 2 endpoints, no conversation list | Video Chat would add signaling endpoints; Fix Known Issues adds GET list |
| `src/Dinder.Api/Program.cs` | 2 SignalR hubs: `/hubs/chat`, `/hubs/notifications` | Video Chat / Speed Dating would add new hub(s) |
| `src/Dinder.Infrastructure/SignalR/ChatHub.cs` | SendMessage, TypingIndicator, MarkRead, JoinConversation, LeaveConversation | Video Chat extends this pattern; Speed Dating needs room-based hubs |
| `src/Dinder.Infrastructure/SignalR/NotificationHub.cs` | Personal user groups, badge updates | Gamification would push achievement/reward notifications |
| `src/Dinder.Infrastructure/Persistence/ChatRepository.cs` | Single-conversation queries only | Fix Known Issues needs `GetConversationsByUserIdAsync()` |
| `src/Dinder.Domain/Entities/Conversation.cs` | Icebreaker columns present, no participant list query | Fix Known Issues wires icebreaker data to frontend |
| `src/Dinder.Domain/Entities/Swipe.cs` | Swipe direction + timestamp | ML Matching uses this as training data; Gamification tracks streaks |
| `src/Dinder.Application/Discovery/Queries/GetCandidatesQuery.cs` | Filter-based (gender, age, distance via ST_DWithin) | ML Matching would add scoring; Gamification would add boost mechanics |
| `src/Dinder.Infrastructure/Persistence/AnalyticsDbContext.cs` | DAU, subscription, swipe metrics | Gamification extends naturally: streaks, achievements |
| `src/app/src/app/core/signalr/signalr.service.ts` | ChatHub + NotificationHub connections | Video Chat adds WebRTC peer connections |
| `src/app/src/app/features/chat/conversation-header.component.ts` | Icebreaker display (unwired) | Fix Known Issues wires this |
| OpenSpec specs (15 total) | All compliant, 2 SHOULD/MAY deferred | Video Chat, ML Matching, Gamification, Speed Dating each need new specs |
| Java/Kotlin/Android toolchain | Java 21.0.10 JDK installed; Kotlin, Gradle, Android SDK NOT installed | Kotlin Mobile needs toolchain setup first |

### Toolchain Audit for Kotlin/Android

```
Java 21.0.10 (OpenJDK):  INSTALLED ✅
javac 21.0.10:           INSTALLED ✅
Kotlin compiler:         NOT INSTALLED ❌
Gradle:                  NOT INSTALLED ❌
Android SDK:             NOT INSTALLED ❌
ANDROID_HOME:            NOT SET ❌
JAVA_HOME:               NOT SET ❌
dotnet 10.0.201:         INSTALLED ✅
Node 24.11.0 / Angular CLI 20.3.8: INSTALLED ✅
Docker 28.4.0:           INSTALLED ✅
```

**Verdict**: Java JDK exists, which is the hardest dependency. Installing Kotlin, Gradle, and Android SDK is feasible (3-4 tooling tasks). BUT the combined effort of toolchain + scaffolding + first feature (auth + discovery) is a standalone phase (10+ tasks). Not suitable to mix with other Phase 4 features.

---

### Candidate Evaluation

#### 1. Video Chat (WebRTC + SignalR signaling)

**Description**: 1-on-1 WebRTC video calls between matched users, gated by match status. Uses SignalR for SDP/ICE signaling exchange. Requires TURN/STUN server infrastructure for NAT traversal.

| Aspect | Assessment |
|--------|------------|
| **Effort** | High (8-12 tasks) |
| **Impact** | Very High — premium differentiator, keeps users in-app |
| **Feasibility** | Medium — SignalR infrastructure exists, but WebRTC adds substantial complexity |
| **Risk** | High — TURN server operations cost, privacy/legal (recording concerns), NAT traversal reliability |
| **Dependencies** | New: WebRTC JS API, coturn/Twilio TURN, `MediaStream` API, new `VideoCallHub` or extend `ChatHub` |
| **New Specs** | 1 (`video-call`) |
| **Test Impact** | ~20 new tests (signaling state machine, match-gating, ICE handling) |

**Pros**:
- Leverages existing SignalR infrastructure (JWT auth, groups, reconnect)
- Match-gated access maps cleanly to existing `IsParticipantAsync()` pattern
- High engagement value — video is a top-requested dating app feature
- Angular WebRTC support is mature (native browser APIs)

**Cons**:
- TURN/STUN server introduces new infrastructure cost and operational complexity
- WebRTC peer connection state machine is complex (offer/answer, ICE candidates, renegotiation)
- Legal/privacy implications for video content (GDPR right-to-access, potential recording)
- Requires significant frontend work (camera permissions, video UI, call controls)
- Testing WebRTC is difficult — needs real browser environments or WebRTC mocks

---

#### 2. ML Matching (Compatibility Scoring)

**Description**: Replace/supplement the current filter-only candidate generation (gender, age, distance via `ST_DWithin`) with a compatibility scoring algorithm. Options: ML.NET (C# native) or Python microservice (scikit-learn/TensorFlow).

| Aspect | Assessment |
|--------|------------|
| **Effort** | Medium-High (6-10 tasks depending on approach) |
| **Impact** | High — better matches → higher retention, more conversations |
| **Feasibility** | Medium — training data exists (swipes, profiles), but cold-start and model tuning are hard |
| **Risk** | Medium-High — ML model quality directly impacts user experience; bad matches drive churn |
| **Dependencies** | ML.NET NuGet (C# path) OR Python + FastAPI + Docker (microservice path) |
| **New Specs** | 1 (`ml-matching`) |
| **Test Impact** | ~15-20 new tests (scoring algorithm, candidate ordering, A/B switch) |

**Approach A: ML.NET (C# native)**
- Pros: No new service, integrates with existing DI pipeline, single deployment unit, C# toolchain already established
- Cons: Limited ML ecosystem compared to Python, fewer pre-built collaborative filtering models, smaller community
- Effort: Medium

**Approach B: Python microservice (FastAPI + scikit-learn)**
- Pros: Rich ML ecosystem, scikit-learn has built-in collaborative filtering (NMF, SVD), easier to iterate on models
- Cons: New service to deploy/monitor/scale, new Docker container, API contract between services, operational complexity
- Effort: High

**Recommendation**: Start with **Approach A (ML.NET)** for a simple weighted scoring model based on profile similarity + swipe patterns. Can graduate to a Python microservice later if needed. Begin with an A/B framework toggle so the scoring can be compared against the current filter-only approach.

---

#### 3. Gamification (Streaks, Achievements, Profile Score, Daily Rewards)

**Description**: Drive DAU through streaks (consecutive daily logins), achievements (badges for profile completion, first match, 100 swipes, etc.), profile completeness score, and daily rewards tied to subscription tiers.

| Aspect | Assessment |
|--------|------------|
| **Effort** | Medium (6-8 tasks) |
| **Impact** | High — gamification is the #1 DAU driver in dating apps (Tinder, Bumble both use it) |
| **Feasibility** | High — existing analytics infrastructure fires all needed events already |
| **Risk** | Low-Medium — additive feature, no breaking changes to core loop |
| **Dependencies** | New entities: `Streak`, `Achievement`, `UserAchievement`, `DailyReward`. New `GamificationDbContext` or extend `AnalyticsDbContext` |
| **New Specs** | 1 (`gamification`) |
| **Test Impact** | ~15-20 new tests (streak calculation, achievement unlocking, reward claiming) |

**Pros**:
- Builds directly on existing event infrastructure (`UserLoggedInEvent` → `TrackDAUHandler`, `SwipeRecordedEvent`, `MatchCreatedEvent`)
- `TrackDAUHandler` already tracks daily logins — streak detection is a natural extension
- Profile completeness score can leverage existing `Profile.UpdateDiscoverability()` logic
- Can integrate with subscription tiers for premium rewards (uses existing `RequiresTierAttribute`)
- Low infrastructure risk — no new services, just new entities + handlers
- Angular: new profile gamification section, achievement toast notifications via `NotificationHub`

**Cons**:
- Needs careful anti-gaming design (can't let users farm rewards)
- Streak calculation requires UTC midnight boundary handling
- Achievement definitions need to be extensible (consider a data-driven approach vs hardcoded)

---

#### 4. Virtual Speed Dating

**Description**: Timed chat rounds in group events. Users join scheduled events, get paired for short timed conversations (3-5 min), then rotate. Host controls for event creation and management.

| Aspect | Assessment |
|--------|------------|
| **Effort** | High (10-15 tasks) |
| **Impact** | Medium-High — novelty factor, but needs critical mass of concurrent users |
| **Feasibility** | Medium — complex state machine, but SignalR group infrastructure exists |
| **Risk** | High — critical mass problem (empty events kill the feature), complex state machine, timezone scheduling |
| **Dependencies** | New entities: `SpeedDatingEvent`, `EventRound`, `EventParticipant`. New hub or extend ChatHub. Scheduled job infrastructure (Hangfire/Quartz). |
| **New Specs** | 1 (`speed-dating`) |
| **Test Impact** | ~25+ new tests (event lifecycle, round transitions, pairing algorithm, host controls) |

**Pros**:
- SignalR groups already handle multi-user rooms (just need event-scoped groups)
- Unique differentiator vs Tinder/Bumble (they don't have this)
- Could be monetized (premium events, priority access)

**Cons**:
- Requires critical mass of concurrent users to be viable — empty events are a bad UX
- Complex state machine: scheduled → active → round → break → round → finished
- Timezone scheduling is notoriously bug-prone
- Needs scheduled job infrastructure (Hangfire, Quartz.NET, or Azure Functions)
- High frontend complexity: event lobby, countdown timers, round transitions
- Pairing algorithm for N participants with gender preferences is non-trivial
- Testing requires simulating multi-user scenarios

---

#### 5. Kotlin Mobile (Native Android)

| Aspect | Assessment |
|--------|------------|
| **Effort** | Very High (12-18 tasks: toolchain + scaffolding + first features) |
| **Impact** | Very High — opens entire mobile user acquisition channel |
| **Feasibility** | Medium — Java JDK is installed, but 3 major toolchain components missing |
| **Risk** | Medium — toolchain issues could block progress |
| **Dependencies** | Kotlin compiler, Gradle, Android SDK, Android emulator or physical device |
| **New Specs** | 0 (reuses existing API specs); new project structure |
| **Test Impact** | N/A until toolchain is fully operational |

**What's needed to get started**:
1. Install Kotlin compiler (2 options: manual download or `sdkman` via WSL) — 1 task
2. Install Android SDK command-line tools — 1 task
3. Set `ANDROID_HOME` and `JAVA_HOME` env vars — 1 task
4. Install Gradle (or use Gradle wrapper in project) — 1 task
5. Scaffold Jetpack Compose project with Material 3 — 1 task
6. Create shared API client (Ktor or Retrofit) consuming existing REST + SignalR endpoints — 2 tasks
7. Build first screen (login/register) — 2 tasks
8. Build discovery screen (swipe cards) — 3 tasks

Total: 12 tasks minimum for toolchain + auth + discovery. This is a standalone Phase 5, not a Phase 4 feature.

---

#### 6. Fix Known Issues (Phase 3 Debt)

**Description**: Implement the missing `GET /api/v1/conversations` endpoint to wire icebreaker data to the Angular frontend, plus any other Phase 3 warnings.

| Aspect | Assessment |
|--------|------------|
| **Effort** | Low (1-2 tasks) |
| **Impact** | Medium — completes IQ-2 (icebreaker display) integration |
| **Feasibility** | Very High — repository method exists, just needs query + endpoint + Angular wiring |
| **Risk** | Very Low — additive endpoint, zero breaking changes |
| **Dependencies** | None — IChatRepository.GetConversationAsync() already works |
| **New Specs** | 0 (closes spec gap in real-time-chat) |
| **Test Impact** | ~3-5 new tests (endpoint authorization, response shape, empty state) |

**What's needed**:
1. Add `GetConversationsByUserIdAsync(Guid userId)` to `IChatRepository` — returns list of conversations with icebreaker data, last message preview, unread count
2. Create `GetConversationsQuery` in `Dinder.Application/Chat/Queries/`
3. Add `GET /api/v1/conversations` to `ChatController`
4. Wire Angular `conversation-header.component.ts` icebreaker display to API response

---

### Recommendation

**Recommended Phase 4 scope**: Fix Known Issues + Gamification (with ML Matching groundwork)

| Priority | Feature | Tasks Est. | Rationale |
|----------|---------|-----------|-----------|
| 1 (P0) | Fix Known Issues | 1-2 | Unblocks Phase 3 IQ-2; very low effort, very low risk |
| 2 (P1) | Gamification | 6-8 | Highest impact/effort ratio; builds on existing analytics; proven DAU driver; low risk |
| 3 (P1) | ML Matching groundwork | 3-4 | Lightweight scoring model with A/B toggle; prepares for full ML in Phase 5 |
| **Total** | | **10-14** | ~7-8 spec requirements, ~35-40 new tests, 400-line review budget: Medium risk (may need 2 PRs) |

**Deferred to Phase 5**: Kotlin Mobile (needs dedicated toolchain phase)
**Deferred to Phase 6+**: Video Chat (high complexity, needs TURN infra), Virtual Speed Dating (critical mass problem)

**Why Gamification over Video Chat?**
1. Gamification is **additive** — no changes to core match/chat flow, no new infrastructure
2. Gamification leverages **existing domain events** that already fire (login, swipe, match)
3. Video Chat needs **new infrastructure** (TURN/STUN server) and has **legal/privacy implications** the team hasn't addressed
4. Gamification **directly drives DAU**, which makes Video Chat more valuable later (more concurrent users = more video calls)
5. Gamification can be **delivered in 1-2 PRs** within the 400-line review budget; Video Chat would require chained PRs

### Risks

- **Gamification anti-gaming**: Users may try to farm rewards by logging in without engagement. Mitigation: require meaningful actions (swipes, messages) for streak credit, not just login.
- **ML cold start**: New users with no swipe history get poor recommendations. Mitigation: start with profile similarity scoring (prompts, interests) before behavioral signals kick in.
- **400-line budget**: 10-14 tasks may produce 500-700 changed lines. Forecast: Medium risk of exceeding 400 lines. Recommend auto-chain into 2 PRs (PR1: Fix Known Issues + Gamification entities/repos; PR2: Gamification handlers + Angular + ML groundwork).
- **Kotlin toolchain drift**: The longer Kotlin mobile is deferred, the more the API surface grows. Mitigation: document the REST API contract explicitly in Phase 4 so mobile can catch up faster in Phase 5.

### Ready for Proposal

**Yes**. The recommendation is clear: Phase 4 = Fix Known Issues + Gamification + ML groundwork. Defer Video Chat, Speed Dating, and Kotlin Mobile to future phases.

Proceed to `sdd-propose` with:
- Change name: `dating-app-phase4`
- Scope: Fix GET /api/v1/conversations, Gamification (streaks + achievements + profile score + daily rewards), ML scoring groundwork (A/B toggle)
- Deferred: Video Chat, Virtual Speed Dating, Kotlin Mobile
- Chained PR strategy: auto-chain (PR1: fix + entities; PR2: handlers + frontend)
