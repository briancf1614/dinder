# Proposal: Dating App Phase 2 — Monetization

## Intent

Monetize Dinder via tiered subscriptions. Phase 1 hardcoded a 50-swipe/day limit with zero tier awareness — users can't pay even if they want to. Phase 2 introduces Free/Plus/Premium tiers with Stripe payment processing, entitlement enforcement, and premium features gated by tier.

## Scope

### In Scope
- Subscription bounded context (Domain, Application, Infrastructure, API)
- Stripe Checkout integration + webhook lifecycle management
- Feature gating via `.NET [RequireEntitlement]` attribute or MediatR pipeline behavior
- Premium features: unlimited swipes, see-who-liked-you, profile boost (1/month), undo swipe
- Angular PWA manifest + basic service worker for mobile browser reach
- Stripe CLI local webhook testing in docker-compose

### Out of Scope
- Native Android (Kotlin/Jetpack Compose) — deferred to Phase 3 (toolchain not installed)
- Mobile IAP receipt validation (Apple/Google 30% cut)
- Read receipts, passport/travel mode, advanced filters
- ML matching, video chat, consumable credits, ad-supported tier

## Capabilities

### New Capabilities
- `subscription-management`: Stripe Checkout + Billing, tier management (Free/Plus/Premium), webhook lifecycle (created, renewed, canceled, expired, refunded)
- `entitlement-enforcement`: Feature gating middleware, premium feature access control, tier-dependent behavior injection

### Modified Capabilities
- `discovery`: DI-4 (Daily Swipe Limit) SHALL become tier-dependent — Free=50/day, Plus/Premium=unlimited
- `identity-access`: User aggregate SHALL carry subscription tier and status for authorization decisions

## Approach

New Subscription bounded context following existing Clean Architecture patterns (CQRS + MediatR, per-context DbContext). Stripe Checkout Session API for payment UI (never handle raw card data). Stripe webhooks sync subscription lifecycle. Entitlement enforcement via MediatR pipeline behavior that checks user tier before command execution. Angular PWA stopgap: `manifest.json` + basic service worker for installability on mobile browsers.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Dinder.Domain/` | New/Modified | Subscription aggregate, tier/status enums, User +tier FK |
| `src/Dinder.Application/Subscription/` | New | CQRS handlers for checkout, webhooks, entitlements |
| `src/Dinder.Application/Discovery/` | Modified | SwipeCommand tier-aware limit; GetCandidatesQuery premium likes |
| `src/Dinder.Infrastructure/Payments/` | New | StripeService, StripeConfiguration |
| `src/Dinder.Infrastructure/Persistence/` | New/Modified | SubscriptionDbContext, SubscriptionRepository, User migration |
| `src/Dinder.Api/Controllers/` | New | SubscriptionController, WebhookController |
| `docker-compose.yml` | Modified | Add Stripe CLI service |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Stripe webhook local testing complex | Med | Stripe CLI + `stripe listen --forward-to` in docker-compose |
| PCI-DSS scope creep | Low | Stripe Checkout — never touch raw card data |
| Subscription lifecycle edge cases (trial expiry, payment failure, refund) | Med | Stripe webhooks cover all states; idempotency keys prevent duplicates |
| Kotlin toolchain unavailable | High | Deferred to Phase 3; Angular PWA as mobile stopgap |

## Rollback Plan

1. Revert DI migration to remove `SubscriptionTier` FK from User (existing `SWIPE_LIMIT_REACHED` behavior is fallback)
2. Deactivate Stripe webhook endpoint in API
3. SubscriptionDbContext schema isolated — drop without affecting other contexts

## Dependencies

- Stripe account with test API keys
- Stripe CLI installed for local development
- Pricing matrix finalized (Free/Plus/Premium features confirmed)

## Success Criteria

- [ ] Users can initiate Stripe Checkout and complete payment
- [ ] Stripe webhooks correctly update subscription status (active, canceled, expired)
- [ ] Free-tier users hit 50-swipe/day limit; Plus/Premium users have unlimited
- [ ] Premium features (see-likes, boost, undo) only accessible with correct tier
- [ ] Angular PWA installable on mobile browsers
- [ ] All existing 95 tests still pass
