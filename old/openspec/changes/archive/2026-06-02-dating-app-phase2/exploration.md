# Exploration: Dating App Phase 2 — Monetization & Mobile

## Current State

### What Phase 1 Delivered (Archived 2026-05-31)

Phase 1 shipped the complete core matching loop as a safety-complete, GDPR-ready web app:

| Layer | Status | Key Details |
|-------|--------|-------------|
| **Backend** | ✅ Complete | .NET 10 Clean Architecture, 8 bounded contexts, CQRS + MediatR |
| **Database** | ✅ Complete | PostgreSQL+PostGIS, per-context schemas, EF Core migrations |
| **Real-time** | ✅ Complete | SignalR (ChatHub, NotificationHub), JWT auth in handshake |
| **Auth** | ✅ Complete | JWT + refresh rotation, Google/Apple social login, token revocation |
| **Tests** | ✅ 95 passing | xUnit + Moq, 47/47 tasks complete, 38/38 requirements verified |
| **Angular** | ⚠️ Scaffold only | Standalone components + Signals structure in place, features not wired |

### Architecture Overview

```
src/
├── Dinder.Api/              # ASP.NET Core host, 8 controllers, 2 SignalR hubs
├── Dinder.Application/      # CQRS handlers per bounded context
│   ├── Identity/            # Register, Login, Refresh, Delete
│   ├── Profile/             # CRUD, photos, preferences
│   ├── Discovery/           # Candidates, Swipe (with 50/day limit), Match
│   ├── Chat/                # Messages, conversations, unmatch
│   ├── Notifications/       # Push dispatch, in-app center
│   ├── Moderation/          # Reports, blocks, bans
│   ├── Admin/               # User lookup, moderation actions
│   └── Media/               # Pre-signed upload URLs, CDN
├── Dinder.Domain/           # 17 entities, 12 enums, 4 domain events, 9 repo interfaces
├── Dinder.Infrastructure/   # 8 EF Core DbContexts, SignalR hubs, JWT, Azure Blob
└── Dinder.Contracts/        # Shared DTOs (Identity folder empty, others not scaffolded)
```

### Monetization-Specific Current State

**The swipe limit is hardcoded.** In `SwipeCommand.cs` line 28:
```csharp
if (dailyCount >= 50)
{
    var resetTime = DateTime.UtcNow.Date.AddDays(1);
    throw new InvalidOperationException($"SWIPE_LIMIT_REACHED:{resetTime:O}");
}
```

There is **no tier awareness, no subscription model, no feature gating**, and no payment integration anywhere in the codebase. The `User` entity has no subscription-related fields. The `DiscoveryContext` has no concept of premium features. The notification type `Promotion` exists as an enum value but is unused.

### Existing Extension Points for Monetization

1. **`User.cs`** — can gain a `SubscriptionTier` field or FK to a `Subscription` entity
2. **`SwipeCommand.cs`** — the 50/day limit is the primary gating point; needs to become tier-aware
3. **`GetCandidatesQuery.cs`** — can be extended for "see who liked you" premium feature
4. **`MatchCreatedEvent`** — already published via MediatR; subscription events would follow the same pattern
5. **`ServiceCollectionExtensions.cs`** — has clear registration pattern: 8 DbContexts, 8 repositories, singleton services
6. **`NotificationType.Promotion`** — existing enum value ready for subscription lifecycle notifications

### Mobile-Specific Current State

| Tool | Status |
|------|--------|
| Kotlin | **Not installed** (`config.yaml` confirms) |
| Gradle | **Not installed** |
| Android SDK | **Not installed** |
| Jetpack Compose | **Not installed** |

The backend exposes 20+ REST endpoints and 2 SignalR hubs. All are consumable by any HTTP/WebSocket client — mobile can consume directly. The Angular PWA works on mobile browsers as a stopgap.

---

## Affected Areas

### New Areas (to create)

- `src/Dinder.Domain/Entities/Subscription.cs` — subscription aggregate
- `src/Dinder.Domain/Entities/SubscriptionTier.cs` — pricing tier definition
- `src/Dinder.Domain/Entities/Entitlement.cs` — feature access record
- `src/Dinder.Domain/Enums/SubscriptionTier.cs` — Free/Plus/Premium enum
- `src/Dinder.Domain/Enums/SubscriptionStatus.cs` — Active/Canceled/Expired/Refunded
- `src/Dinder.Domain/Interfaces/ISubscriptionRepository.cs`
- `src/Dinder.Application/Subscription/` — CQRS handlers
- `src/Dinder.Infrastructure/Persistence/SubscriptionDbContext.cs`
- `src/Dinder.Infrastructure/Persistence/SubscriptionRepository.cs`
- `src/Dinder.Infrastructure/Payments/StripeService.cs`
- `src/Dinder.Api/Controllers/SubscriptionController.cs`
- `src/Dinder.Api/Controllers/WebhookController.cs` — Stripe webhook receiver

### Modified Areas

- `src/Dinder.Domain/Entities/User.cs` — add subscription tier reference
- `src/Dinder.Application/Discovery/Commands/SwipeCommand.cs` — tier-aware limit
- `src/Dinder.Application/Discovery/Queries/GetCandidatesQuery.cs` — "who liked you" premium
- `src/Dinder.Domain/Events/` — new subscription lifecycle events
- `src/Dinder.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — register new services
- `src/Dinder.Api/Program.cs` — map new controller routes
- `docker-compose.yml` — add Stripe CLI for local webhook testing
- `openspec/config.yaml` — update `testing.runner`, add Stripe dependency

### NOT Affected (clean boundaries)

- **Chat** — no monetization impact (read receipts premium is future)
- **Moderation** — no monetization impact
- **Admin** — gains subscription lookup but no structural changes
- **Media** — no monetization impact
- **SignalR hubs** — no changes needed

---

## Approaches: Monetization

### Approach 1: Tiered Subscription with Stripe (Recommended)

Classic freemium dating app model. Stripe handles payment processing, webhooks handle lifecycle events, feature gating via middleware/attribute.

**Architecture**:
```
User ──has──▶ Subscription (Tier, Status, StripeCustomerId, PeriodStart/End)
Subscription ──has-many──▶ Entitlement (FeatureKey, IsActive)
```

**Feature Gating**: A .NET `[RequireEntitlement("unlimited_swipes")]` attribute or MediatR pipeline behavior checks entitlements before command execution.

**Tiers**:
| Feature | Free | Plus | Premium |
|---------|------|------|---------|
| Swipes/day | 50 | Unlimited | Unlimited |
| See who liked you | ❌ | ✅ | ✅ |
| Boost profile | ❌ | 1/month | 1/week |
| Undo swipe | ❌ | ✅ | ✅ |
| Passport (change location) | ❌ | ❌ | ✅ |
| Advanced filters | ❌ | ✅ | ✅ |
| Read receipts | ❌ | ❌ | ✅ |

- **Pros**: Proven model (Tinder, Bumble, Hinge), Stripe handles PCI-DSS, webhooks for lifecycle
- **Cons**: Mobile IAP (Apple/Google 30% cut) requires separate receipt validation — deferred to Phase 3
- **Effort**: **Medium** — Stripe SDK is mature, but webhook handling + entitlement system + DB schema is non-trivial

### Approach 2: Consumable Credits Model

Users buy swipe "packs" or "boosts" as one-time purchases instead of subscriptions.

- **Pros**: Simpler than recurring billing, lower Stripe complexity
- **Cons**: Lower LTV, doesn't match dating app norms, less predictable revenue
- **Effort**: **Low-Medium** — no recurring billing lifecycle

### Approach 3: Ad-Supported Free Tier

Show ads in the free tier, remove ads with paid tier. No current ad infrastructure.

- **Pros**: Monetizes free users immediately
- **Cons**: Requires ad network integration (complex), degrades UX, dating apps generally don't do this well
- **Effort**: **High** — ad mediation is complex

### Recommendation: Approach 1 (Tiered Subscription)

Approach 1 is the industry standard and matches what users expect. The Phase 1 domain exploration already scoped this correctly. Stripe's .NET SDK (Stripe.net) is mature and well-documented. The Clean Architecture makes adding a `Subscription` bounded context straightforward — follow the same pattern as the 8 existing contexts.

---

## Approaches: Mobile

### Approach 1: Kotlin + Jetpack Compose Now (Blocked)

Build the Android app using the planned Kotlin/Jetpack Compose stack alongside Phase 2 monetization.

- **Pros**: Native Android experience, full Play Store eligibility, Material 3 design
- **Cons**: **Toolchain NOT installed** — Kotlin, Gradle, Android SDK all missing. Installing and configuring is a full-day task. Cannot develop or test on this machine without setup
- **Effort**: **High** — requires toolchain installation before any code can be written
- **Status**: BLOCKED until toolchain is set up

### Approach 2: Angular PWA as Mobile Stopgap (Recommended)

Leverage the existing Angular 20 app as a Progressive Web App accessible on mobile browsers. Add to Home Screen, offline support, push notifications via service workers.

- **Pros**: Zero new toolchain, leverages existing code, immediate mobile reach, installable PWA
- **Cons**: Not on Play Store, limited native capabilities (camera, GPS work fine in PWA), push notifications require service worker setup
- **Effort**: **Low** — PWA config is mostly manifest + service worker registration

### Approach 3: Defer Mobile to Phase 3 (Phase 1 Plan)

Follow the original Phase 1 plan: web-first MVP → monetize (Phase 2) → native mobile (Phase 3).

- **Pros**: No distraction from monetization, toolchain can be installed when needed
- **Cons**: Users asking for mobile won't get it yet
- **Effort**: **Low** — defer entirely

### Recommendation: Hybrid (Approach 2 now + Approach 1 later)

Ship Phase 2 with an enhanced Angular PWA (manifest, install prompt, basic offline support). This gives mobile users immediate access. Defer native Android (Kotlin/Jetpack Compose) to Phase 3 when the toolchain is installed. The backend APIs are already fully REST/SignalR — any mobile client can consume them. The PWA approach validates the mobile UX before investing in native.

---

## Risks

### Technical Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| **Kotlin/Android toolchain not installed** | **High** | Defer native mobile to Phase 3. Use Angular PWA as stopgap. Install toolchain only when Phase 3 begins. |
| **Stripe account not configured** | **High** | External dependency — requires Stripe account creation, API keys, webhook endpoint setup. Flag early as blocker. |
| **PCI-DSS compliance** | Medium | Use Stripe Elements for web, Stripe SDK for server — never handle raw card data. |
| **Webhook reliability** | Medium | Stripe webhooks need idempotency handling, retry logic, and a local testing setup (Stripe CLI). |
| **Mobile IAP receipt validation** | Medium | Apple/Google in-app purchase requires server-side receipt validation. Different from Stripe flow. Defer to Phase 3. |
| **Entitlement caching stale** | Low | Cache tier info per request with refresh on subscription change. Keep invalidation simple. |
| **Feature flag complexity** | Low | Start with simple tier-based if/switch, not a full feature flag system. |

### Domain Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| **Subscription lifecycle edge cases** | Medium | Trial expiry, payment failure, refund, cancellation — each needs handling. Stripe webhooks cover these. |
| **Free tier dissatisfaction** | Low | 50 swipes/day is generous for a new app; can adjust later. |
| **Pricing model mismatch with market** | Low | Research competitor pricing before launch. Can adjust tiers without code changes (config-based). |

### Unknowns

| Unknown | Why |
|---------|-----|
| **Stripe webhook local testing** | Need Stripe CLI or ngrok for local development. Not configured yet. |
| **Angular PWA push notifications** | Browser push (Web Push API) is different from FCM/APNs native push. May need separate notification path. |
| **Mobile IAP vs Stripe reconciliation** | If web uses Stripe and mobile uses IAP, need unified subscription state. Adds complexity. |

---

## Recommendation

### Phase 2 Scope (Recommended)

**IN**: Subscription system (Stripe), feature gating/entitlements, premium features (unlimited swipes, see who liked you, boost, undo swipe), Angular PWA enhancements for mobile.

**OUT**: Native Android (Kotlin/Jetpack Compose), mobile IAP (Apple/Google), read receipts premium, passport/travel mode, advanced filters.

### Rationale

1. **Monetization first** — the swipe limit is already in place at 50/day; upgrading it to tier-aware unlocks the revenue model without changing user behavior
2. **Mobile PWA stopgap** — the Angular app already exists; adding PWA manifest + service worker gives mobile reach now without blocking on toolchain install
3. **Defer native mobile** — Kotlin/Gradle/Android SDK are not installed. Installing and learning them is a separate effort that would delay monetization. The backend is API-complete and ready for any client.
4. **Keep Phase 2 lean** — 4-6 weeks focused on the revenue engine. Don't dilute with mobile native, video chat, ML matching, or other Phase 3 features.

### Pre-requisites Before Starting

- [ ] **Stripe account** created with test API keys
- [ ] **Stripe CLI** installed for local webhook testing
- [ ] **Pricing finalized** — Free/Plus/Premium feature matrix locked
- [ ] **Kotlin decision** — confirm deferring native Android to Phase 3

---

## Ready for Proposal

**Yes.** The monetization domain is well-understood, the codebase is thoroughly mapped, the extension points are clear, and the risks are documented. The architect should proceed to `sdd-propose` for "dating-app-phase2" with the recommended scope above.
