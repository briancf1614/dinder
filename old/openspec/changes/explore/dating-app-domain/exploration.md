# Exploration: Dating App Domain (Dinder / MeetMatch)

## 1. Domain Analysis

The dating app domain breaks into these subdomains, each with distinct responsibilities, data ownership, and evolution cycles.

### 1.1 User Onboarding & Profile Creation

**Purpose**: Convert a visitor into an active user with a complete profile.

**Core flows**:
- Registration (email, Google, Apple, Facebook OAuth)
- Email verification
- Photo upload (multi-photo, ordering, caption)
- Bio text (character-limited, with prompts as optional templates)
- Preferences: gender identity, interested-in, age range, max distance
- Onboarding wizard UX: step-by-step or single-page?
- Phone number verification (optional but recommended for trust)
- Profile completeness scoring

**Data owned**: User aggregate (identity, profile, preferences, photos, verification status)

**Edge cases**: Under-18 rejection, banned-email detection, duplicate-account detection, photo moderation before profile goes live

### 1.2 Discovery & Matching

**Purpose**: Show users potential matches and let them express interest.

**Core flows**:
- Card stack (swipe left/right)
- Filters: age range, distance, gender
- Boost: temporary profile visibility increase
- "Who liked you": see users who already expressed interest
- Mutual match detection (both swiped right → match)
- Super-like / highlight mechanisms
- Undo last swipe (premium)
- Daily swipe limits (free tier) vs unlimited (premium)
- Re-show previously passed profiles (premium, time-gated)

**Data owned**: Swipe events, match records, discovery preferences, active filters

**Algorithm considerations**:
- Simple: random within filters with recency bias (dislike seeing same person)
- Moderate: collaborative filtering (people who liked A also liked B)
- Advanced: ML-based (Elo-like score, engagement prediction) — NOT for MVP
- Freshness: never show the same profile twice in a session
- Active-user bias: prioritize recently active users

### 1.3 Real-Time Chat / Messaging

**Purpose**: Enable matched users to communicate.

**Core flows**:
- 1-on-1 chat between matches only (mutual match required)
- Text messages delivered in real time
- Read receipts
- Typing indicators
- Media sharing: photos, GIFs (no unsolicited explicit content)
- Message history (infinite scroll / pagination)
- Unmatch → chat history hidden (but retained for moderation)
- Ice-breaker prompts (optional)

**Data owned**: Messages, conversation metadata, read-state, attachments

### 1.4 Authentication & Identity

**Purpose**: Prove who you are and control access.

**Core flows**:
- Email/password registration with ASP.NET Identity
- Social login: Google, Apple (mandatory for App Store), Facebook
- JWT access tokens + refresh tokens
- Token rotation on refresh
- Two-factor (SMS/authenticator) — v2 feature
- Password reset flow
- Account deletion (GDPR right to erasure)

**Data owned**: User credentials, external login mappings, refresh tokens, audit log

**Note**: Apple Sign-In is MANDATORY for App Store if you offer any other social login. This is not optional for iOS.

### 1.5 Notifications

**Purpose**: Re-engage users and inform them of events.

**Core flows**:
- Push notifications: new match, new message, "someone liked you"
- Email notifications: digest, re-engagement ("you have 3 new likes"), account changes
- In-app notification center: persistent bell icon + badge count
- Notification preferences: per-type opt-out
- Quiet hours
- Rate limiting to avoid spam

**Data owned**: Notification templates, user preferences, delivery logs, device tokens

### 1.6 Safety & Moderation

**Purpose**: Keep users safe and maintain platform quality.

**Core flows**:
- Report user (with reason: harassment, fake profile, spam, inappropriate photos)
- Block user (one-way, instant)
- Photo moderation: automated (AI/ML) + manual review queue
- Content filtering on chat messages
- Shadow-ban / soft-ban for suspicious accounts
- Underage detection and immediate ban
- Terms of Service acceptance tracking

**Data owned**: Reports, blocks, moderation actions, flagged content, bans

**Critical**: This is NOT a nice-to-have. Dating apps without safety features are legally and reputationally dangerous.

### 1.7 Premium Subscriptions

**Purpose**: Monetize through tiered feature access.

**Core flows**:
- Subscription tiers: Free, Plus, Premium/Platinum
- Payment integration: Stripe (web), in-app purchase (iOS/Android)
- Feature gating: check entitlement before feature use
- Trial period management
- Subscription lifecycle: activate, renew, cancel, expire, refund
- Receipt validation (server-side for mobile IAP)

**Features by tier**:
| Feature | Free | Plus | Premium |
|---------|------|------|---------|
| Swipes/day | 50 | Unlimited | Unlimited |
| See who liked you | ❌ | ✅ | ✅ |
| Boost profile | ❌ | 1/month | 1/week |
| Undo swipe | ❌ | ✅ | ✅ |
| Passport (change location) | ❌ | ❌ | ✅ |
| Advanced filters | ❌ | ✅ | ✅ |
| Read receipts | ❌ | ❌ | ✅ |

### 1.8 Admin Panel / Moderation Dashboard

**Purpose**: Staff tools for support, moderation, and analytics.

**Core flows**:
- User search / lookup by email, ID, phone
- Review reported users and take action (warn, suspend, ban)
- Photo moderation queue (approve/reject)
- View user's recent activity (swipes, messages, reports)
- Analytics dashboard: DAU/MAU, matches/day, messages/day, revenue
- Feature flag management
- Content management (prompts, onboarding questions)

**Data owned**: Admin audit log, moderation decisions, analytics aggregates

---

## 2. Bounded Contexts (DDD)

Following Domain-Driven Design, the domain decomposes into these bounded contexts:

### Core Domains (competitive advantage — build in-house)

#### 2.1 Identity & Access Context
**Role**: Core
**Owns**: User, UserRole, ExternalLogin, RefreshToken, UserSession
**Integrates with**: All other contexts (provides user identity)
**Key aggregate**: User (UserId, Email, Phone, CreatedAt, IsVerified, IsBanned)
**API surface**: Register, Login, RefreshToken, LinkExternalLogin, DeleteAccount

#### 2.2 Profile Context
**Role**: Core
**Owns**: Profile, Photo, Preference, Interest
**Integrates with**: Identity (userId), Discovery (for filters), Media Storage
**Key aggregate**: Profile (ProfileId, UserId, DisplayName, Bio, Gender, InterestedIn, Birthday, Location)
**API surface**: CreateProfile, UpdateBio, UploadPhoto, ReorderPhotos, SetPreferences

#### 2.3 Discovery Context
**Role**: Core (the heart of the product)
**Owns**: SwipeEvent, Match, DiscoveryFilter, DiscoverySession, Boost
**Integrates with**: Profile (candidate pool), Identity (current user), Premium (limits/features)
**Key aggregate**: DiscoverySession (session per user per time window, generates candidate queue)
**API surface**: GetCandidates, SwipeRight, SwipeLeft, UndoLastSwipe, ActivateBoost, GetWhoLikedMe

#### 2.4 Communication Context
**Role**: Core
**Owns**: Conversation, Message, MessageReadState, TypingIndicator
**Integrates with**: Identity, Match (from Discovery), Media Storage
**Key aggregate**: Conversation (ConversationId, MatchId, Participants[], Messages[], CreatedAt)
**API surface**: GetConversations, GetMessages, SendMessage, MarkRead, Unmatch, SendIcebreaker

#### 2.5 Notification Context
**Role**: Supporting (critical but not differentiating)
**Owns**: Notification, DeviceToken, NotificationPreference, DeliveryLog
**Integrates with**: Discovery (new-match events), Communication (new-message events), Identity (user targeting)
**API surface**: RegisterDevice, GetNotifications, MarkRead, UpdatePreferences, SendPush

### Supporting Domains (important but not core differentiator)

#### 2.6 Subscription Context
**Role**: Supporting
**Owns**: Subscription, PricingPlan, PaymentMethod, Invoice
**Integrates with**: Identity (userId), Discovery (feature gating)
**External**: Stripe (web), Apple/Google IAP (mobile)
**API surface**: GetPlans, Subscribe, Cancel, GetEntitlements, ValidateReceipt

#### 2.7 Moderation & Safety Context
**Role**: Supporting
**Owns**: Report, Block, ModerationAction, Ban, PhotoModerationQueue
**Integrates with**: Identity, Profile, Communication (message content)
**API surface**: ReportUser, BlockUser, ReviewReport, BanUser, ApprovePhoto

#### 2.8 Media Context
**Role**: Generic (commodity)
**Owns**: MediaFile, MediaMetadata, UploadUrl
**Integrates with**: Profile (photos), Communication (shared media)
**External**: Azure Blob Storage / AWS S3
**API surface**: GetUploadUrl, ConfirmUpload, DeleteMedia, GetSignedUrl

### Context Map

```
Identity ◄── Profile ◄── Discovery ◄── Communication
    │          │            │              │
    │          │            ▼              │
    │          │      Subscription         │
    │          │                           │
    ▼          ▼            ▼              ▼
  ┌─────────────────────────────────────────┐
  │         Notification Context            │
  │  (listens to events from all others)     │
  └─────────────────────────────────────────┘

Moderation ──► Profile, Communication (reads content, writes bans)
Media ◄── Profile, Communication (serves files)
Admin ──► All contexts (read + moderation actions)
```

Context relationships:
- **Identity → All**: Every context needs to know "who is the current user"
- **Profile → Discovery**: Discovery reads profiles to build candidate pool
- **Discovery → Communication**: A Match triggers conversation creation
- **Discovery → Notification**: Match event triggers push
- **Communication → Notification**: Message event triggers push
- **Identity → Moderation**: Banning a user queries Identity
- **All → Subscription**: Feature gating checks entitlements

---

## 3. MVP Definition

### What's IN (Professional MVP)

A **professional** MVP is NOT a prototype. It ships to real users with real data. It MUST be:
- Secure (auth, data isolation)
- Moderated (report, block at minimum)
- Reliable (no lost matches, no lost messages)
- GDPR-compliant (data export, account deletion)
- On the App Store / Play Store (Apple Sign-In required)

#### Phase 1: Core Loop (8-12 weeks, 2 devs)

| Module | Scope |
|--------|-------|
| **Auth** | Email/password + Google + Apple Sign-In, JWT tokens, refresh token rotation |
| **Profile** | Photos (up to 6), bio, gender, interested-in, birthday, location |
| **Discovery** | Swipe card stack, basic filters (age, distance, gender), mutual match, 50 swipes/day free |
| **Chat** | 1-on-1 text messages, real-time via SignalR, message history, unmatch |
| **Notifications** | Push notifications for matches and messages (FCM + APNs) |
| **Safety** | Report user, block user, basic photo moderation (manual queue) |
| **Admin** | User lookup, report review, ban/unban |
| **Infrastructure** | Docker Compose (API + DB + SignalR), PostgreSQL + PostGIS, Azure Blob for photos |
| **Web Frontend** | Angular 20 responsive web app, all core flows |
| **Mobile** | Deferred to v2 (see below) |

#### Phase 2: Monetization & Polish (4-6 weeks after Phase 1)

| Module | Scope |
|--------|-------|
| **Subscriptions** | Stripe integration, Free/Plus/Premium tiers, feature gating |
| **Premium Features** | See who liked you, boost profile, undo swipe, unlimited swipes |
| **Photo Moderation** | Automated NSFW detection (Azure Content Moderator or similar) |

#### Phase 3: Mobile (6-8 weeks)

| Module | Scope |
|--------|-------|
| **iOS** | Native Kotlin Multiplatform or SwiftUI (decision needed) |
| **Android** | Kotlin + Jetpack Compose (as planned) |
| **Mobile Auth** | Apple Sign-In on iOS, Google Sign-In on Android |

### What's OUT (v2+)

| Feature | Rationale |
|---------|-----------|
| Video chat | High infra cost, moderation nightmare, low MVP value |
| ML-based matching | Cold-start problem, needs data to train |
| Social graph (friends of friends) | Privacy complexity, not core loop |
| Events/group dating | Separate product almost |
| Voice messages in chat | Nice but not MVP-critical |
| "Incognito mode" | Premium feature, not MVP |
| Travel/passport mode | Premium feature |
| Advanced ML moderation | Start manual, add AI later |
| Gamification/badges | Distraction from core loop |
| WebRTC calling | See video chat |
| AI-generated conversation starters | Nice but not core |

### User Journey (Phase 1)

```
1. Land on website
2. Register with Google/Apple/Email
3. Complete profile wizard:
   a. Upload photos (min 1, max 6)
   b. Write bio
   c. Set gender + interested in
   d. Set age range + distance
4. Browse cards → swipe right/left
5. MATCH! → notification received
6. Open chat → send message
7. Receive reply → real-time notification
8. Report or block if uncomfortable
9. Delete account → all data erased within 30 days (GDPR)
```

### Platform Priority

**Web first**. Why:
- Single codebase to iterate fast
- Angular is already installed and configured
- Mobile toolchain (Kotlin, Gradle, Android SDK) is NOT installed
- Mobile adds App Store review, IAP complexity, push notification certs
- Web PWA can work on mobile browsers as stopgap
- Mobile v2 with Jetpack Compose (Android) + Kotlin Multiplatform or SwiftUI (iOS)

---

## 4. Technical Approach Recommendations

### 4.1 Architecture: Modular Monolith (Clean Architecture)

```
src/
├── Dinder.Api/               # ASP.NET Core host, controllers, middleware
├── Dinder.Application/        # Use cases, CQRS handlers, DTOs
├── Dinder.Domain/             # Entities, aggregates, value objects, domain events
├── Dinder.Infrastructure/     # EF Core, SignalR hubs, external services
│   ├── Persistence/
│   ├── RealTime/
│   ├── Storage/
│   └── Auth/
└── Dinder.Modules/           # Vertical slices (one per bounded context)
    ├── Identity/
    ├── Profile/
    ├── Discovery/
    ├── Communication/
    ├── Notification/
    └── Moderation/
```

**Decision**: Modular monolith first, NOT microservices.
- **Rationale**: 2-person team, no scale problems yet, faster iteration
- **Extraction points**: Each `Module` folder is a future microservice boundary
- **Communication pattern**: In-process domain events (MediatR) → can become message bus later
- **Database**: Shared PostgreSQL but separate schemas per module (logical separation)

### 4.2 Database Design

**Decision**: PostgreSQL with Entity Framework Core + Npgsql.

**Schema strategy**: One PostgreSQL schema per bounded context:
- `identity.user`, `identity.refresh_tokens`, `identity.external_logins`
- `profile.profiles`, `profile.photos`, `profile.preferences`
- `discovery.swipes`, `discovery.matches`, `discovery.candidates`
- `communication.conversations`, `communication.messages`, `communication.read_states`
- `notification.devices`, `notification.preferences`, `notification.log`
- `moderation.reports`, `moderation.blocks`, `moderation.bans`

**Geolocation**: PostGIS extension (built into PostgreSQL).
```sql
CREATE EXTENSION postgis;
-- profile has a geography(Point, 4326) column
-- proximity query: ST_DWithin(location, @center, @radius_meters)
```

**Why not document DB (MongoDB)?**
- Relationships matter (user → profile → photos → swipes → matches → conversations)
- ACID transactions for match creation (swipe + match insert must be atomic)
- Reporting/admin queries benefit from SQL
- EF Core is already in the stack
- Can add Redis for caching candidate queues later

### 4.3 Real-Time Chat

**Decision**: SignalR (ASP.NET Core built-in).

SignalR is the right choice because:
- Already part of ASP.NET Core, zero new dependencies
- WebSocket transport with fallback (server-sent events, long polling)
- Azure SignalR Service for scale-out when needed (trivial upgrade path)
- Built-in connection management, groups, user mapping
- Works with JWT auth (access token in connection handshake)

**Scale-out plan**: Start in-process → Azure SignalR Service when >1 instance.

### 4.4 Image Storage

**Decision**: Azure Blob Storage (or AWS S3 — same pattern).

**Flow**:
1. Client requests a pre-signed upload URL from API
2. Client uploads directly to blob storage (not through API)
3. Client notifies API with blob key
4. API triggers async moderation pipeline
5. API returns CDN URL once moderation passes

**Why not local disk**:
- Docker container restarts lose data
- Doesn't scale beyond one instance
- CDN delivery for images is critical for UX

### 4.5 Matching Algorithm (MVP)

**Decision**: Simple filter-based with recency bias. No ML.

```
SELECT p.*
FROM profile.profiles p
WHERE p.user_id != @current_user_id
  AND p.gender = ANY(@interested_in)
  AND p.birthday BETWEEN @min_age_date AND @max_age_date
  AND ST_DWithin(p.location, @center, @max_distance_meters)
  AND p.user_id NOT IN (
    SELECT swiped_user_id FROM discovery.swipes
    WHERE swiper_user_id = @current_user_id
  )
  AND p.is_active = true
  AND p.is_shadow_banned = false
ORDER BY p.last_active_at DESC
LIMIT 20;
```

**Evolution path**:
- v1: Above query
- v2: Add preference weighting (activity score)
- v3: Add collaborative filtering once enough data exists
- v4: ML model for match quality prediction

### 4.6 Authentication

**Decision**: ASP.NET Identity Core + JWT (access/refresh token pattern).

- ASP.NET Identity handles user store, password hashing, role management
- JWT access tokens (short-lived, 15 min)
- Refresh tokens (long-lived, 30 days, stored in DB, revokable)
- External login via `Microsoft.AspNetCore.Authentication.Google` and `Apple`
- Social login mapping stored in `AspNetUserLogins` table

### 4.7 API Design

**Decision**: REST with OpenAPI (Swagger) documentation.

- REST for CRUD operations (profile, preferences, reports)
- SignalR for real-time (chat, typing indicators, notifications)
- Pagination: cursor-based for infinite scroll (discovery, messages)
- Versioning: URL-based (`/api/v1/...`)
- Rate limiting: AspNetCoreRateLimit middleware

### 4.8 Frontend Architecture (Angular 20)

```
src/app/
├── core/                  # Singleton services, guards, interceptors
│   ├── auth/
│   ├── signalr/
│   └── http/
├── features/              # Feature modules (lazy-loaded)
│   ├── onboarding/
│   ├── discovery/
│   ├── chat/
│   ├── profile/
│   ├── settings/
│   └── admin/
├── shared/                # Reusable components, pipes, directives
│   ├── components/
│   └── pipes/
└── app.component.ts       # Shell: nav, notification bell, auth gate
```

- Standalone components (no NgModules)
- Signals for state management (no NgRx for MVP — keep it simple)
- Route guards for auth, profile-completion, subscription
- SignalR service injected at root, reconnects on token refresh

---

## 5. Competitor/Industry Reference

### Standard User Expectations (table stakes)

From Tinder, Bumble, Hinge — these are NOT differentiators, they're baseline:

| Feature | Expected? | Notes |
|---------|-----------|-------|
| Swipe card UI | ✅ | Industry standard, users expect it |
| Mutual match → chat | ✅ | Universal pattern |
| Photo-first profiles | ✅ | Photos are the primary decision factor |
| Location-based filtering | ✅ | Dating is inherently local |
| Push notifications | ✅ | Required for re-engagement |
| Report & block | ✅ | Safety baseline, app stores require it |
| Social login (Google/Apple) | ✅ | Reduces registration friction |
| Free tier with limits | ✅ | Freemium is the dominant model |
| Photo verification | 🟡 | Bumble has it, Tinder has it — increasingly expected |
| Video chat | 🟡 | Tinder/Hinge/Bumble have it — COVID-era addition |
| AI conversation starters | 🟡 | Bumble has AI openers — new trend |
| Incognito mode | 🟡 | Tinder/Bumble — premium feature |

### Architectural Signals (from public engineering blogs & talks)

- Tinder: Originally monolith on Java, evolved to microservices. Geo-indexing with GeoHash. Elo score was replaced with engagement-based scoring.
- Bumble: Backend mostly Python/Django on AWS. Real-time via WebSockets. PostgreSQL + Redis.
- Hinge: Node.js backend. Heavy emphasis on ML for "Most Compatible" feature.

### What Makes a Dating App "Professional" vs "Toy"

| Dimension | Toy | Professional |
|-----------|-----|-------------|
| Auth | Email only | Social login, password reset, account deletion |
| Photos | Direct upload, no moderation | Signed URLs, async moderation pipeline, CDN |
| Safety | No report/block | Report, block, photo moderation, shadow-banning |
| Real-time | Polling or no chat | SignalR WebSocket, typing indicators, read receipts |
| Data Privacy | No GDPR | GDPR-compliant: export, deletion, consent tracking |
| Scaling | Single server, no plan | Docker Compose with clear scale-out path |
| Payments | None or hardcoded | Stripe integration with webhook handling |
| Admin | No admin | User lookup, moderation queue, ban system |
| Error Handling | Generic 500 errors | Structured problem details, rate limiting, logging |
| Testing | None | Unit + integration tests, testcontainers for DB |

---

## 6. Risks & Unknowns

### Technical Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| **PostGIS spatial query performance at scale** | Medium | Start simple, add GiST indexes, benchmark with 100K+ profiles |
| **SignalR connection management at scale** | Medium | Azure SignalR Service as scale-out path; test connection lifecycle thoroughly |
| **Photo moderation pipeline complexity** | High | Start with manual queue, add Azure Content Moderator API, budget 1 week for pipeline |
| **Token refresh race conditions** | Medium | Queue refresh requests, invalidate old refresh tokens, test concurrent requests |
| **Apple Sign-In App Store requirement** | High | Mandatory — must implement before iOS launch. For web MVP, Google + email is sufficient |
| **GDPR compliance (right to erasure)** | High | Design data model with soft-delete + hard-delete cascade from day one. Account deletion MUST cascade to all user data. |
| **Cold start problem (empty candidate pool)** | Low | Seed some test profiles for dev; production launches need marketing push for critical mass |
| **Kotlin/Android toolchain not installed** | Low | MVP is web-first, mobile comes later. Install toolchain only when Phase 3 begins. |

### Domain Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| **Underage users** | Critical | Date of birth validation (must be 18+), report mechanism, immediate ban on suspicion |
| **Harassment & safety** | Critical | Block, report, message filtering, photo moderation — day one features, NOT v2 |
| **Fake profiles / bots** | Medium | Email verification, photo moderation, behavioral heuristics (v2), report mechanism |
| **Payment compliance (PCI-DSS)** | High | Use Stripe/Elements — never handle raw card data. Server-side receipt validation for mobile IAP. |

### Unknowns Requiring Prototyping

| Unknown | Why Prototype? |
|---------|----------------|
| **SignalR + JWT refresh mid-connection** | Does the SignalR connection survive token rotation? Need to test reconnect with new token. |
| **PostGIS query performance with Gin index** | Need benchmark: how fast is `ST_DWithin` on 50K, 100K, 500K profiles? |
| **Angular Signals for complex state** | Signals are relatively new (Angular 16+). Need to validate they handle chat scroll-back + real-time update patterns. |
| **Photo upload + moderation pipeline latency** | What's acceptable UX between upload and profile going live? Can moderation be async? |
| **Cross-platform push notification reliability** | FCM + APNs integration tested end-to-end with real devices. |

---

## 7. Recommendations Summary

### Architecture Decision Record (ADR) — Pre-Proposal

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Architecture | Modular monolith (Clean Architecture) | Team size 2, faster iteration, extraction points for future |
| Database | PostgreSQL + PostGIS | Relational fits, PostGIS is best-in-class geo, already in stack |
| Real-time | SignalR (in-process) | Built-in, zero-cost, Azure SignalR for scale-out |
| ORM | Entity Framework Core | Already in stack, Npgsql provider mature |
| Auth | ASP.NET Identity + JWT | Built-in, secure defaults, social login support |
| Image Storage | Azure Blob Storage | CDN delivery, pre-signed uploads, cost-effective |
| API Style | REST + OpenAPI | Standard, tooling-rich, SignalR for real-time only |
| Frontend State | Angular Signals | No extra dependency, adequate for MVP complexity |
| Mobile | Deferred to Phase 3 | Web-first reduces complexity, mobile toolchain not ready |
| Monorepo | Single repo, solution folders | Simpler CI/CD, shared contracts possible |

### What to Prototype First

Before writing the first line of production code:

1. **SignalR + JWT connection lifecycle** — verify reconnect with token refresh
2. **PostGIS proximity query** — benchmark with synthetic 100K profiles
3. **Photo upload flow** — pre-signed URL → blob → moderation callback
4. **Angular Signals + real-time updates** — validate chat message stream pattern

---

## Ready for Proposal

**Yes**. The domain is well-understood, bounded contexts are clear, MVP scope is defined, and key technical decisions have rationale. 

**Next step**: `sdd-propose` to formalize the "Dating App MVP — Phase 1 Core Loop" change proposal with formal scope, exclusions, and stakeholder sign-off points.
