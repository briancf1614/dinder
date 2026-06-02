# Mobile Identity Specification

## Purpose

Native Android authentication screens consuming the existing identity-access backend API. Handles login, registration, JWT lifecycle, social login (Google), and account deletion — with zero backend changes.

## Requirements

| ID | Requirement | API |
|----|-------------|-----|
| MI-1 | Login (Email / Google) | POST /identity/login, /login/external |
| MI-2 | Registration with Age Gate | POST /identity/register |
| MI-3 | JWT Lifecycle — secure storage + auto-refresh | POST /identity/refresh |
| MI-4 | Session Restoration on App Launch | — |
| MI-5 | Account Deletion | DELETE /identity/account |

### MI-1: Login Screen (Email + Google)

The app MUST present email/password login and Google Sign-In on a single unauthenticated screen. On success, tokens MUST be persisted to EncryptedSharedPreferences. The app SHALL display server error messages (401, 409) to the user.

#### Scenario: Email login success

- GIVEN user enters valid email and password
- WHEN they tap "Log In"
- THEN access/refresh tokens are stored in EncryptedSharedPreferences
- AND user navigates to the discovery screen

#### Scenario: Google Sign-In — new user

- GIVEN user taps "Continue with Google" and completes consent
- WHEN the identity token is sent to POST /identity/login/external
- THEN a new account is auto-created and tokens are stored
- AND user navigates to discovery screen

#### Scenario: Invalid credentials

- GIVEN user enters wrong password
- WHEN they tap "Log In"
- THEN 401 response is displayed as an inline error

### MI-2: Registration with Age Gate

The app MUST validate password complexity (min 8 chars, 1 uppercase, 1 digit) client-side before submission. Birthday field SHALL use a Material 3 date picker. Age gate (<18) errors (422) MUST show the server message.

#### Scenario: Successful registration

- GIVEN user provides unique email, valid password, and birthday ≥18
- WHEN they submit registration
- THEN tokens are stored and user navigates to profile setup

### MI-3: JWT Lifecycle — Storage + Auto-Refresh

The app MUST store tokens in EncryptedSharedPreferences. Access token expiry (15 min) SHALL be checked before every API call. Expired access tokens MUST trigger an automatic refresh using the stored refresh token. Refresh failures (401) SHALL clear tokens and redirect to login. Token reuse detection (full revocation) SHALL force re-authentication.

#### Scenario: Expired access token auto-refreshed

- GIVEN access token expired but refresh token is valid
- WHEN any authenticated API call is made
- THEN a refresh call succeeds silently
- AND the original request is retried with the new access token

### MI-4: Session Restoration on App Launch

The app MUST check for stored tokens on cold start. Valid tokens SHALL skip the login screen entirely. Expired access tokens with valid refresh SHALL auto-refresh before showing the main UI.

#### Scenario: Returning user with valid session

- GIVEN user has stored, unexpired tokens
- WHEN the app launches (cold start)
- THEN login screen is skipped and discovery loads immediately

### MI-5: Account Deletion

The app MUST show a confirmation dialog before calling DELETE /identity/account. On success, tokens SHALL be cleared and the user returned to the login screen.

#### Scenario: User deletes account

- GIVEN authenticated user on settings screen
- WHEN they confirm deletion and the API returns 204
- THEN all stored tokens are cleared and the login screen is shown
