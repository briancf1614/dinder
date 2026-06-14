# Entitlement Enforcement Specification

## Purpose

Gate features by subscription tier using a MediatR pipeline behavior that checks user entitlements before command execution. Premium features unlock on activation and revoke on cancellation or expiry.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| EE-1 | Tier-to-Feature Mapping | MUST |
| EE-2 | Entitlement Middleware | MUST |
| EE-3 | Entitlement Activation | MUST |
| EE-4 | Entitlement Revocation | MUST |

### EE-1: Tier-to-Feature Mapping

The system MUST enforce the following feature matrix:

| Feature | Free | Plus | Premium |
|---------|------|------|---------|
| Daily swipes | 25 | 100 | Unlimited |
| See-who-liked-you | No | Yes | Yes |
| Profile boost | No | No | 1/mo |
| Undo swipe | No | Yes | Yes |

A feature SHALL NOT be accessible below its minimum tier. Higher tiers implicitly include all lower-tier features.

#### Scenario: Free user requests premium feature

- GIVEN a Free-tier user
- WHEN they request the "see-who-liked-you" endpoint
- THEN the request is rejected with 403 Forbidden
- AND the response body indicates the required tier (Plus)

### EE-2: Entitlement Middleware

The system MUST implement a MediatR `IPipelineBehavior` that reads the user's tier from JWT claims (without a DB round-trip at the gate step) and rejects commands annotated with `[RequiresTier(Tier.Plus)]` when the user's tier is insufficient. The 403 response body MUST include `RequiredTier` and `CurrentTier` string fields enabling client-side messaging (e.g., "This feature requires Plus. You are on Free.").

#### Scenario: Check bypasses DB — JWT-only at gate

- GIVEN a Plus-tier user with a valid JWT containing `tier: Plus`
- WHEN they invoke a `[RequiresTier(Tier.Plus)]` command
- THEN the pipeline behavior authorizes the command
- AND no database query is performed at the gate step

#### Scenario: Tier-inadequate command rejected with metadata

- GIVEN a Free-tier user with a valid JWT containing `tier: Free`
- WHEN they invoke a `[RequiresTier(Tier.Premium)]` command
- THEN the pipeline behavior rejects with 403 Forbidden
- AND the response body contains `RequiredTier: "Premium"` and `CurrentTier: "Free"`
- AND the command handler is never invoked

#### Scenario: Tier-inadequate for same-tier-gated feature

- GIVEN a Free-tier user with a valid JWT containing `tier: Free`
- WHEN they invoke a `[RequiresTier(Tier.Plus)]` command
- THEN the pipeline behavior rejects with 403 Forbidden
- AND the response body contains `RequiredTier: "Plus"` and `CurrentTier: "Free"`

### EE-3: Entitlement Activation

The system MUST unlock premium features immediately upon subscription activation (webhook `checkout.session.completed`). The next JWT refresh SHALL include the updated tier.

#### Scenario: Entitlements active after subscription

- GIVEN a Free user who just completed Plus checkout
- WHEN the activation webhook is processed and they refresh their JWT
- THEN their next request to a Plus-gated endpoint succeeds

### EE-4: Entitlement Revocation

The system MUST revoke premium entitlements when a subscription is canceled or expires. The next JWT refresh SHALL reflect the downgraded tier. Active sessions MAY retain entitlements until their current JWT expires (max 15 minutes).

#### Scenario: Canceled subscription loses entitlements

- GIVEN a Premium user whose subscription is canceled
- WHEN the cancellation webhook is processed and their JWT expires
- THEN their next JWT refresh carries `tier: Free`
- AND subsequent requests to Premium-gated endpoints are rejected
