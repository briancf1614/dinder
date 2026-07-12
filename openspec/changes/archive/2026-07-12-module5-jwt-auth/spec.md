# Delta Specs: Module 5 — JWT Identity

## identity-register

### RegisterCommand Creates User with Hashed Password

The handler MUST hash the password with BCrypt, save a new User, and return a JWT with email claim.

**Scenario**: GIVEN a valid email and password; WHEN Register is called; THEN a User is persisted with hashed password AND a JWT is returned containing the email claim.

**Scenario**: GIVEN an existing email; WHEN Register is called; THEN a ValidationException is thrown with "Email already registered".

### Password Never Stored in Plain Text

The stored PasswordHash MUST be a BCrypt hash (60-char string starting with `$2a$` or `$2b$`). The raw password MUST never be logged or returned.

**Scenario**: GIVEN a registered user; WHEN inspecting the database; THEN PasswordHash is a BCrypt string, NOT the raw password.

---

## identity-login

### LoginCommand Validates Credentials and Returns Token Pair

The handler MUST verify the password against the stored BCrypt hash. On success, it MUST return a JWT (15-minute expiry) and a refresh token (7-day expiry, stored in User entity).

**Scenario**: GIVEN valid email and password; WHEN Login is called; THEN a JWT and refresh token are returned.

**Scenario**: GIVEN wrong password; WHEN Login is called; THEN an UnauthorizedAccessException is thrown.

**Scenario**: GIVEN unknown email; WHEN Login is called; THEN an UnauthorizedAccessException is thrown (same error as wrong password — don't leak user existence).

---

## identity-refresh

### RefreshCommand Rotates Token

The handler MUST validate the refresh token stored in the User entity. On success, it MUST generate a new JWT AND a new refresh token (rotation). The old refresh token is invalidated.

**Scenario**: GIVEN a valid refresh token; WHEN Refresh is called; THEN a new JWT and new refresh token are returned.

**Scenario**: GIVEN an invalid/expired refresh token; WHEN Refresh is called; THEN UnauthorizedAccessException is thrown.

---

## identity-me

### GET /me Returns Authenticated User Info

The endpoint MUST be protected with `[Authorize]`. It MUST extract the email from the JWT and return the user's Id, Email, and CreatedAt.

**Scenario**: GIVEN a valid JWT in Authorization header; WHEN GET /me is called; THEN 200 with JSON containing id, email, and createdAt.

**Scenario**: GIVEN no JWT; WHEN GET /me is called; THEN 401 Unauthorized.
