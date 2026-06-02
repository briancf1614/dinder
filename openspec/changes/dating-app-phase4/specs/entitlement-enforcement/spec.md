# Delta for Entitlement Enforcement

## MODIFIED Requirements

### EE-2: Entitlement Middleware

The system MUST implement a MediatR `IPipelineBehavior` that reads the user's tier from JWT claims (without a DB round-trip at the gate step) and rejects commands annotated with `[RequiresTier(Tier.Plus)]` when the user's tier is insufficient. The 403 response body MUST include `RequiredTier` and `CurrentTier` string fields enabling client-side messaging (e.g., "This feature requires Plus. You are on Free.").

(Previously: Rejected with 403 but response body lacked `RequiredTier` + `CurrentTier` metadata.)

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
