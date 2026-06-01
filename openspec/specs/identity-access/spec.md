# Identity & Access Specification

## Purpose

Manage user registration, authentication, and account lifecycle. Provides JWT-based access control and GDPR-compliant account deletion across all bounded contexts.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| IA-1 | Email/Password Registration | MUST |
| IA-2 | Social Login (Google, Apple) | MUST |
| IA-3 | JWT Access Token Issuance | MUST |
| IA-4 | Refresh Token Rotation | MUST |
| IA-5 | GDPR Account Deletion Cascade | MUST |
| IA-6 | Token Rejection on Revocation | MUST |

### IA-1: Email/Password Registration

The system MUST allow users to register with email and password. Email MUST be verified before full access. Duplicate emails MUST be rejected at registration.

#### Scenario: Successful registration

- GIVEN a visitor provides a unique email and password meeting complexity requirements (min 8 chars, 1 uppercase, 1 digit)
- WHEN they submit the registration form
- THEN a user account is created with email-verification-pending status
- AND a verification email is dispatched

#### Scenario: Duplicate email rejected

- GIVEN a user already exists with `alice@example.com`
- WHEN a visitor attempts to register with the same email
- THEN registration is rejected with 409 Conflict and a generic "email unavailable" message
- AND no new account is created

### IA-2: Social Login (Google, Apple)

The system MUST support Google Sign-In for web. Apple Sign-In SHALL be implemented for App Store compliance before any iOS launch. First-time social login MUST auto-create a local user account with the external provider mapping.

#### Scenario: Google Sign-In — new user

- GIVEN a visitor with no existing account
- WHEN they complete Google Sign-In successfully
- THEN a new user account is created with the Google external login mapping
- AND a short-lived JWT access token and a refresh token are issued

#### Scenario: Apple Sign-In — returning user

- GIVEN a user previously registered via Apple Sign-In
- WHEN they authenticate via Apple Sign-In again
- THEN the existing account is matched via the external login mapping
- AND new access and refresh tokens are issued

### IA-3: JWT Access Token Issuance

The system MUST issue JWT access tokens with a 15-minute expiry containing the user's ID and roles. Every authenticated request MUST validate the token.

#### Scenario: Valid token authorizes request

- GIVEN a user holds a valid, unexpired JWT access token
- WHEN they call a protected API endpoint
- THEN the request is authorized and the user identity is available to the handler

#### Scenario: Expired token rejected

- GIVEN a user's JWT access token has expired
- WHEN they call a protected API endpoint
- THEN the request is rejected with 401 Unauthorized

### IA-4: Refresh Token Rotation

The system MUST issue a new refresh token on each use and invalidate the previous one. Refresh tokens MUST be stored server-side and be revokable. Reuse of a revoked token SHALL trigger revocation of all active tokens for that user (indicating potential token theft).

#### Scenario: Successful token refresh

- GIVEN a user holds a valid, unrevoked refresh token
- WHEN they call the refresh endpoint
- THEN a new access token and a new refresh token are issued
- AND the previous refresh token is invalidated

#### Scenario: Reused refresh token triggers full revocation

- GIVEN an attacker reuses a refresh token that was already rotated
- WHEN the server detects the reuse
- THEN all active refresh tokens for that user are revoked immediately
- AND the user must re-authenticate

### IA-5: GDPR Account Deletion Cascade

The system MUST support full account deletion per GDPR Article 17. Deletion MUST cascade to all user data across contexts: profile, photos, swipes, matches, messages, device tokens. A soft-delete with up to 30 days retention MAY precede hard deletion.

#### Scenario: User initiates account deletion

- GIVEN an authenticated user
- WHEN they submit an account deletion request with confirmation
- THEN the account is soft-deleted immediately (access revoked)
- AND all associated data is queued for hard deletion within 30 days
- AND all active sessions and refresh tokens are revoked

### IA-6: Token Rejection on Revocation

The system MUST reject access and refresh tokens for banned or soft-deleted accounts immediately, regardless of token expiry.

#### Scenario: Banned user token rejected

- GIVEN a user account that was banned 1 hour ago
- WHEN a previously issued (still unexpired) JWT is presented
- THEN the request is rejected with 403 Forbidden
