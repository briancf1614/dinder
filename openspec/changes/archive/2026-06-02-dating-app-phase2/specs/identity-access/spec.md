# Delta for Identity & Access

## ADDED Requirements

### IA-7: User Tier in JWT Claims

The system MUST include the user's subscription tier (`Free`, `Plus`, `Premium`) as a `tier` claim in every JWT access token. Entitlement enforcement SHALL use this claim for fast authZ without a database round-trip. The claim MUST be present in all tokens: new issuance and refreshes.

#### Scenario: JWT contains tier claim

- GIVEN a Plus-tier user authenticating
- WHEN a JWT access token is issued
- THEN the token payload includes `"tier": "Plus"`
- AND the claim is signed and verifiable

#### Scenario: JWT refresh preserves tier

- GIVEN a Premium user whose subscription is still active
- WHEN they refresh their access token
- THEN the new token includes `"tier": "Premium"`

## MODIFIED Requirements

### IA-3: JWT Access Token Issuance

The system MUST issue JWT access tokens with a 15-minute expiry containing the user's ID, roles, and subscription tier. Every authenticated request MUST validate the token.

(Previously: JWT contained only user ID and roles — no tier claim.)

#### Scenario: Valid token authorizes request

- GIVEN a user holds a valid, unexpired JWT access token with tier claim
- WHEN they call a protected API endpoint
- THEN the request is authorized and user identity (including tier) is available to the handler

#### Scenario: Expired token rejected

- GIVEN a user's JWT access token has expired
- WHEN they call a protected API endpoint
- THEN the request is rejected with 401 Unauthorized
