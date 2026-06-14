# Tasks: Dating App Phase 2 — Monetization

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 1200–1400 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1: Foundation (~350) → PR 2: CQRS+API (~450) → PR 3: Premium+PWA+Tests (~500) |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain types, JWT tier claim, Stripe infra, DB context, DI | PR 1 | Base: `feature/dating-app-phase2`; SM-1, IA-7 |
| 2 | Subscription CQRS, webhook handler, controllers, EntitlementBehavior | PR 2 | Base: PR 1 branch; SM-2, SM-3, SM-4, EE-2 |
| 3 | Tier-aware swipe, premium discovery endpoints, PWA, tests | PR 3 | Base: PR 2 branch; DI-4, EE-1 |

## Phase 1: Foundation — Domain & Infrastructure (SM-1, IA-7)

- [x] 1.1 Create `SubscriptionTier` enum (Free, Plus, Premium) and `SubscriptionStatus` enum (Active, PastDue, Canceled, Expired) — SM-1, SM-4
- [x] 1.2 Create `Subscription` aggregate entity (Id, UserId, StripeSubscriptionId, Tier, Status, CurrentPeriodEnd) with status progression methods — SM-1, SM-4
- [x] 1.3 Add `SubscriptionTier Tier = Free` + `string? StripeCustomerId` to `User.cs` — SM-1
- [x] 1.4 Create `IStripeService` (CreateCheckoutSession, CreatePortalSession, ConstructWebhookEvent) and `ISubscriptionRepository` interfaces — SM-2
- [x] 1.5 Create `RequiresTierAttribute` — `[RequiresTier(SubscriptionTier.Plus)]` marker on `IRequest` types — EE-1
- [x] 1.6 Modify `IJwtService.GenerateAccessToken` to accept `string tier` param; add `tier` claim to JWT payload — IA-7
- [x] 1.7 Create `StripeConfiguration` reading `Stripe:SecretKey`, `Stripe:WebhookSecret`, price IDs from `IConfiguration` — SM-2
- [x] 1.8 Implement `StripeService` — Stripe.Checkout.Session, BillingPortal.Session, EventUtility.ConstructEvent — SM-2, SM-3
- [x] 1.9 Create `SubscriptionDbContext` (schema `subscription`), `SubscriptionConfiguration` (EF), `SubscriptionDbContextFactory`, `SubscriptionRepository` — SM-1
- [x] 1.10 Modify `UserConfiguration` — add `Tier` (string conv) and `StripeCustomerId` columns — SM-1, IA-7
- [x] 1.11 Modify `LoginCommand` and `RefreshTokenCommand` to pass `user.Tier` to `GenerateAccessToken` — IA-7
- [x] 1.12 Register `SubscriptionDbContext`, `StripeService`, `ISubscriptionRepository` in `ServiceCollectionExtensions.AddInfrastructure()` — SM-2

## Phase 2: Subscription Service & API (SM-2, SM-3, SM-4, EE-2)

- [x] 2.1 Create `CreateCheckoutSessionCommand` + handler — reject 409 if already subscribed to same tier; call StripeService; return sessionUrl — SM-2
- [x] 2.2 Create `CreatePortalSessionCommand` + handler — return Stripe Customer Portal URL — SM-2
- [x] 2.3 Create `GetSubscriptionStatusQuery` + handler — return tier, status, CurrentPeriodEnd — SM-4
- [x] 2.4 Create `ProcessStripeWebhookCommand` — handle `checkout.session.completed` (activate), `customer.subscription.updated` (past_due → 7d grace), `customer.subscription.deleted` (revert Free); idempotent via StripeSubscriptionId upsert — SM-3, SM-4
- [x] 2.5 Create `EntitlementBehavior` (MediatR `IPipelineBehavior`) — read `tier` from JWT claims, check `[RequiresTier]` attribute, reject 403 if insufficient; no DB round-trip at gate — EE-2
- [x] 2.6 Create `SubscriptionController` — `POST checkout`, `GET status`, `POST portal` — SM-2, SM-4
- [x] 2.7 Create `WebhookController` — `POST stripe` (unauth, raw body, `Stripe-Signature` verify via EventUtility) — SM-3
- [x] 2.8 Register `EntitlementBehavior` in MediatR pipeline (Program.cs); enable raw body buffering for webhook route; add `EnableSubscriptions` feature flag — EE-2, SM-3

## Phase 3: Discovery Premium Features (DI-4, EE-1)

- [x] 3.1 Modify `SwipeCommand` — tier-aware limit: Free=25, Plus=100, Premium=unlimited; 429 response includes `upgrade_url` — DI-4
- [x] 3.2 Create `UndoSwipeCommand` with `[RequiresTier(Plus)]` — removes last swipe, decrements daily counter — EE-1
- [x] 3.3 Create `GetLikesQuery` with `[RequiresTier(Plus)]` — returns users who right-swiped current user, excluding already-swiped — EE-1
- [x] 3.4 Create `BoostCommand` with `[RequiresTier(Premium)]` — 1/month limit enforced; bumps profile in candidate results — EE-1
- [x] 3.5 Add `POST /discovery/undo`, `GET /discovery/likes`, `POST /discovery/boost` to `DiscoveryController` — EE-1

## Phase 4: Mobile Reach, DevOps & Testing

- [x] 4.1 Add Stripe CLI service to `docker-compose.yml` (`stripe listen --forward-to api:8080/api/v1/webhooks/stripe`) — SM-3
- [x] 4.2 Create Angular PWA `manifest.json` + `ngsw-config.json` in `src/app/` for mobile browser installability
- [x] 4.3 Run EF migration `AddSubscriptionTier` on identity schema (DinderDbContext) — SM-1, IA-7
- [x] 4.4 Run EF migration `InitialSubscription` on subscription schema (SubscriptionDbContext) — SM-1
- [x] 4.5 Unit test: `EntitlementBehavior` rejects Free user from `[RequiresTier(Plus)]` command, passes Plus user — EE-2
- [x] 4.6 Unit test: `SwipeCommand` tier-aware limits (Free 25th swipe passes, 26th returns 429+upgrade_url; Premium unlimited) — DI-4
- [x] 4.7 Unit test: Subscription status progression (active → past_due → 7d grace → expired → Free) — SM-4
- [x] 4.8 Integration test: SubscriptionDbContext writes + Stripe webhook idempotency (replayed event is no-op) — SM-3
- [x] 4.9 Integration test: JWT tier claim round-trip (login → tier in token; refresh → tier preserved) — IA-7
- [x] 4.10 Contract test: new REST endpoints via Swashbuckle OpenAPI; verify all 141 existing tests pass
