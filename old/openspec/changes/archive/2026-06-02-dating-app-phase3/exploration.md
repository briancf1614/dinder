# Exploration: Dating App Phase 3 — Next Features

## Current State

### What Exists (Phase 1 + Phase 2 complete)

Dinder is a dating app with 9 bounded contexts, 11 specs, 141 passing tests, and a functional core loop:

| Context | Status | Key capabilities |
|---------|--------|-----------------|
| `identity-access` | Complete | Email/Google auth, JWT + refresh rotation, GDPR deletion, tier claim in JWT |
| `user-profile` | Complete | Photos (≤6, pre-signed upload), bio, gender, preferences, PostGIS geolocation, age gate |
| `discovery` | Complete | Candidate generation (gender + age + ST_DWithin filter), swipe recording, mutual match detection, tier-aware swipe limits (25/100/unlimited) |
| `real-time-chat` | Complete | SignalR 1-on-1 messaging, read receipts, unmatch, cursor-paginated history, match-gated access |
| `notifications` | Complete | FCM+APNs push, in-app center, device tokens, per-type opt-out, MediatR event handlers |
| `safety-moderation` | Complete | Report, block, manual photo review queue, ban/unban, audit log |
| `admin-dashboard` | Complete | User search, report queue, ban/unban, append-only audit log |
| `media-storage` | Complete | Pre-signed uploads (Azure Blob), confirmation, CDN delivery, GDPR cascade |
| `subscription-management` | Complete | Stripe Checkout, webhook lifecycle (active/past_due/canceled/expired), Free/Plus/Premium tiers |
| `entitlement-enforcement` | Complete | MediatR pipeline behavior, JWT tier claim, `[RequiresTier]` attribute, 15-min revocation window |

### What's Powerful

- **Clean Architecture modular monolith**: Per-context PostgreSQL schemas (`identity.*`, `discovery.*`, etc.), MediatR domain events. Every module is extractable to a microservice later.
- **Real-time infrastructure**: SignalR fully integrated with JWT auth. Chat and notifications already use it.
- **Monetization is done**: Stripe subscriptions work, tier gating works. Users CAN pay.
- **Spatial queries**: PostGIS `ST_DWithin` + GiST index already proven for candidate filtering.
- **Moderation pipeline**: Manual photo review queue with `PendingReview → Approved/Rejected` workflow. Admin audit log is append-only.

### What's Missing / Weak

- **Discovery is purely rule-based**: Candidate generation is `WHERE gender IN (...) AND age BETWEEN x AND y AND ST_DWithin(...)`. No scoring, no ranking beyond "last active recency." No personalization, no learning.
- **Frontend is a skeleton**: Angular exists with core services (auth, SignalR, HTTP interceptor, routes) but feature components are directory stubs with no implementation. Any Phase 3 feature requires significant frontend work.
- **No automated content moderation**: All photo moderation is manual. No NSFW detection, no AI filtering.
- **No business intelligence**: No analytics, no dashboards for growth metrics, no retention tracking.
- **No social depth**: Profiles are bare (bio + photos). No prompts, no icebreakers, no conversation starters.
- **No Kotlin toolchain**: Java 21 is installed but Kotlin, Gradle, and Android SDK are absent. Native mobile is blocked.

## Kotlin / Android Feasibility Check

```
Java 21 (OpenJDK):  INSTALLED  ✓
Kotlin compiler:    NOT FOUND  ✗
Gradle:             NOT FOUND  ✗
Android SDK:        NOT FOUND  ✗ (ANDROID_HOME not set)
JAVA_HOME:          NOT SET    ✗
```

**Assessment**: Java 21 is present, which means the Kotlin toolchain CAN be installed. However, this requires:

1. Install Kotlin compiler (`sdk install kotlin` or Chocolatey)
2. Install Gradle (`choco install gradle`)
3. Install Android SDK + set `ANDROID_HOME` + accept licenses
4. Set `JAVA_HOME` to the OpenJDK 21 path

This is a **half-day setup task**, not a blocker. But even after setup, building a Kotlin/Jetpack Compose mobile app replicates all existing Angular PWA functionality — it's a second frontend, not new features. The PWA already covers mobile browser reach.

**Verdict**: Kotlin native mobile should wait until there's enough user traction to justify the dual-frontend maintenance burden. The Angular PWA is the pragmatic mobile strategy for now.

## Candidate Features — Evaluated

| # | Feature | Effort | Impact | Feasibility | Risk |
|---|---------|--------|--------|-------------|------|
| 1 | Safety — Automated Moderation + Photo Verification | Medium | **High** | High | Low — extends existing pipeline |
| 2 | Social — Profile Prompts + Icebreakers | **Low** | Medium | **Very High** | **Very Low** — pure CRUD on profile context |
| 3 | Analytics Dashboard | Medium | **High** | High | Medium — perf impact on write path |
| 4 | Advanced Matching (ML scoring) | **High** | Very High | Medium | **High** — cold-start, no training data |
| 5 | Gamification (streaks, achievements) | Medium | Medium | High | Medium — can feel gimmicky |
| 6 | Video Chat (WebRTC + SignalR) | **High** | High | Medium | **High** — NAT traversal, TURN infra, complex testing |
| 7 | Events / Speed Dating | **Very High** | Medium | Low | **Very High** — room orchestration nightmare |

### 1. Safety — Automated Moderation + Photo Verification

**What**: Replace manual-only photo review with AI-powered NSFW detection (Azure AI Vision or similar). Add selfie+liveness verification to prove real identity. Builds directly on existing `safety-moderation` and `media-storage` pipelines.

- **Pros**: Dramatically improves platform trust; competitive differentiator; leverages existing `PhotoReview` entity and moderation queue; Azure AI Vision integrates via REST SDK
- **Cons**: Azure AI Vision adds ~$0.001/image cost; liveness check is complex UX (camera access, face matching); false positives can frustrate users
- **Effort**: Medium — new `MediaVerificationService`, AI integration, liveness check endpoint, Angular camera component
- **Affected areas**: `MediaFile` entity (+`VerificationStatus`), `PhotoReview` workflow (auto-approve/flag), `ModerationRepository`, new `POST /media/verify-selfie` endpoint

### 2. Social — Profile Prompts + Icebreakers

**What**: Add Hinge-style profile prompts ("My ideal first date is...", "I'll know it's time to delete this app when...") and icebreaker questions that appear after matching. Pure data additions to profile and chat contexts.

- **Pros**: Very low effort — two new string properties on Profile + one on Conversation; no new bounded context; massive UX improvement; drives conversation quality; zero infrastructure changes
- **Cons**: Purely additive — not a differentiator alone; requires Angular UI work (profile edit, discovery card display, icebreaker picker)
- **Effort**: Low — `Profile` entity gains `Prompts: List<ProfilePrompt>`, `Conversation` gains `IcebreakerQuestion`, new migration, Angular UI components
- **Affected areas**: `Profile.cs` (+`ProfilePrompt` value object), `Conversation.cs` (+`IcebreakerQuestion`), `CreateOrUpdateProfileCommand`, `GetCandidatesQuery` (include prompts in `CandidateDto`), `profile`, `discovery`, and `chat` Angular features

### 3. Analytics Dashboard

**What**: Admin dashboard expansion with business metrics: user growth (daily/weekly), subscription conversion rate, match rate, retention cohorts, swipe volume. Event-driven analytics via MediatR handlers that write to a lightweight analytics schema.

- **Pros**: Enables data-driven product decisions; high business value from day one; builds on existing admin infrastructure; PostgreSQL is adequate for analytics at this scale
- **Cons**: Write-path performance impact if not async; chart rendering is non-trivial on frontend; retention cohorts are complex SQL
- **Effort**: Medium — new `analytics` schema, `AnalyticsDbContext`, event handlers (`MatchCreated → IncrementMatchCount`, `SwipeRecorded → IncrementSwipeCount`), aggregation queries, chart UI
- **Affected areas**: New `AnalyticsDbContext` + schema, `MatchCreatedEvent`, `SwipeCommand` handler (publish `SwipeRecorded` event), admin Angular feature (+charts), aggregation endpoints

### 4. Advanced Matching (ML Scoring)

**What**: Replace or augment the current rule-based candidate generation with a scored recommendation system. Could use ML.NET for collaborative filtering or a lightweight scoring model based on profile similarity, swipe history patterns, and preference weight learning.

- **Pros**: Core differentiator — "the algorithm" is what makes dating apps sticky; .NET-native via ML.NET (no Python microservice needed); can start simple (weighted scoring) and evolve to ML
- **Cons**: **Cold-start problem**: no real user data yet; ML model requires training data volume; complex A/B testing infrastructure needed; over-engineered for current scale; ML.NET requires NuGet package but no workload
- **Effort**: High — data pipeline, feature engineering, model training, A/B test framework, scoring service integration with `GetCandidatesQuery`
- **Affected areas**: `GetCandidatesQuery` (scoring layer), new `ScoringService`, event collection for training data, `DiscoveryDbContext` (+interaction history tables), Angular (no change needed — backend-only)

### 5. Gamification

**What**: Streak tracking (consecutive days active), achievements (e.g., "5 matches in a day", "sent 100 messages"), profile completeness percentage. New `gamification` bounded context.

- **Pros**: Proven engagement driver; natural fit for dating app; can gate some achievements by tier (Premium-only badges); no external dependencies
- **Cons**: Can feel gimmicky/desperate in a dating context; "grinding for achievements" misaligns with dating goals; requires careful messaging
- **Effort**: Medium — new bounded context, achievement definitions, streak tracker, Angular badge/profile-completeness UI
- **Affected areas**: New `Gamification` context (entities, repository, commands), `UserProfile` (+`ProfileCompletenessScore` computed property), `discovery` and `profile` Angular features

### 6. Video Chat (WebRTC)

**What**: 1-on-1 WebRTC video calls gated by mutual match. SignalR for signaling (offer/answer/ICE candidate relay). TURN/STUN server for NAT traversal in production.

- **Pros**: Keeps users on-platform (vs. moving to WhatsApp/Instagram); competitive differentiator; SignalR already in place for signaling channel
- **Cons**: TURN/STUN server is non-trivial infra (Coturn or cloud service); WebRTC peer connection management is complex; testing across NAT types is painful; bandwidth costs at scale; requires camera/mic permissions UX
- **Effort**: High — WebRTC signaling hub, peer connection management, TURN/STUN config, Angular video component, call management (ring, accept, decline, hangup)
- **Affected areas**: New `VideoHub` (SignalR), `Conversation` +`VideoCall` entity, `video` Angular feature, Docker Compose (+Coturn or cloud TURN config)

### 7. Events / Speed Dating

**What**: Timed virtual speed dating events where users join rooms and rotate through 3-5 minute chat rounds. After each round, users indicate interest. Mutual interest reveals the match.

- **Pros**: Novel feature — few dating apps do this well; creates urgency and social proof; good marketing hook
- **Cons**: Extreme orchestration complexity — room assignment, timer synchronization, round rotation, late join handling, disconnection recovery, match reveal; very hard to test; needs critical mass of simultaneous users
- **Effort**: Very High — `Event` aggregate, `RoomManager` service, timed SignalR group management, round scheduler, interest collection, reveal logic, Angular event lobby + chat UI
- **Affected areas**: New `Events` bounded context, new SignalR hub, complex state machine, significant Angular work

## Recommendation

### Phase 3 Scope: Safety + Social + Analytics

Prioritize **three complementary features** that collectively deliver high impact with manageable risk:

| Priority | Feature | Rationale |
|----------|---------|-----------|
| **P1** | Social — Profile Prompts + Icebreakers | Quickest win. Leverages existing Profile/Conversation entities. Immediately improves UX and conversation quality. No infra changes. |
| **P2** | Safety — Automated Photo Moderation | Builds on existing moderation pipeline. AI integration is a REST call. Dramatically improves platform trustworthiness. |
| **P3** | Analytics Dashboard | Enables data-driven iteration. New `analytics` schema is isolated. Event-driven via MediatR — no write-path blocking. |

**Combined effort**: Medium (estimated 3-5 implementation sessions)
**Combined risk**: Low — all three extend existing bounded contexts without architectural change

### Why NOT the others

- **Advanced Matching**: Premature without real user data. Revisit in Phase 4 when there's interaction history to train on.
- **Video Chat**: High infrastructure complexity. SignalR is ready but WebRTC signaling + TURN/STUN is a separate beast. Gate behind "Phase 4 if DAU > X."
- **Gamification**: Nice-to-have but risks feeling gimmicky. Let social features prove engagement before adding game mechanics.
- **Events/Speed Dating**: Extreme complexity. Only viable at scale with critical mass. Not a Phase 3 feature.

### Kotlin Path

Java 21 is installed — the missing pieces are installable. But Kotlin native mobile should be a **Phase 4 decision**, gated by user traction metrics (e.g., "30% of users access via mobile browser, retention drop on mobile"). The Angular PWA is the pragmatic mobile strategy now.

## Risks

- **Frontend debt**: Angular feature directories exist but have zero implementation. Every Phase 3 feature requires building frontend components from scratch. Budget ~40% of effort for Angular work.
- **Analytics write-path performance**: Event handlers must be truly async (fire-and-forget via `INotification`) to avoid slowing down the swipe/match flow. Already proven pattern in existing `MatchCreatedNotificationHandler`.
- **AI moderation false positives**: Automated NSFW detection will occasionally flag legitimate photos. Must retain human override in the admin queue. Rejected photos must include clear appeal messaging.
- **Stripe test keys missing**: Phase 2's Stripe integration may be untested on this machine. Phase 3 doesn't touch Stripe, but any Phase 2 regression must be caught.

## Ready for Proposal

**Yes** — the scope is clear and the recommendations are concrete. The orchestrator should proceed to `sdd-propose` with change name `dating-app-phase3` and the recommended scope: Social Features (profile prompts + icebreakers), Safety (automated photo moderation), and Analytics Dashboard.
