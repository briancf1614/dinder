# Identity Spec

## domain-identity

### Requirements

#### RegisterCommand Creates User with Hashed Password

The handler MUST hash the password with BCrypt, save a new User, and return a JWT with email claim.

**Scenario**: GIVEN a valid email and password; WHEN Register is called; THEN a User is persisted with hashed password AND a JWT is returned containing the email claim.

**Scenario**: GIVEN an existing email; WHEN Register is called; THEN a ValidationException is thrown with "Email already registered".

#### Password Never Stored in Plain Text

The stored PasswordHash MUST be a BCrypt hash (60-char string starting with `$2a$` or `$2b$`). The raw password MUST never be logged or returned.

**Scenario**: GIVEN a registered user; WHEN inspecting the database; THEN PasswordHash is a BCrypt string, NOT the raw password.

#### LoginCommand Validates Credentials and Returns Token Pair

The handler MUST verify the password against the stored BCrypt hash. On success, it MUST return a JWT (15-minute expiry) and a refresh token (7-day expiry, stored in User entity).

**Scenario**: GIVEN valid email and password; WHEN Login is called; THEN a JWT and refresh token are returned.

**Scenario**: GIVEN wrong password; WHEN Login is called; THEN an UnauthorizedAccessException is thrown.

**Scenario**: GIVEN unknown email; WHEN Login is called; THEN an UnauthorizedAccessException is thrown (same error as wrong password — don't leak user existence).

#### RefreshCommand Rotates Token

The handler MUST validate the refresh token stored in the User entity. On success, it MUST generate a new JWT AND a new refresh token (rotation). The old refresh token is invalidated.

**Scenario**: GIVEN a valid refresh token; WHEN Refresh is called; THEN a new JWT and new refresh token are returned.

**Scenario**: GIVEN an invalid/expired refresh token; WHEN Refresh is called; THEN UnauthorizedAccessException is thrown.

#### User Entity Has Profile Columns

The User entity MUST include four new nullable columns: DisplayName (string, max 100), Bio (string, max 500), BirthDate (DateOnly), and Gender (enum: Male, Female, NonBinary, Other). These columns MUST default to null and remain null until explicitly set via PUT /me/profile.

**Scenario**: GIVEN a user registers with email and password only; WHEN the User entity is persisted; THEN DisplayName, Bio, BirthDate, and Gender are null.

#### GET /me Returns Authenticated User Info

The endpoint MUST be protected with `[Authorize]`. It MUST extract the email from the JWT and return the user's Id, Email, CreatedAt, DisplayName, Bio, BirthDate, and Gender. Profile fields that are null MUST be returned as JSON null.

**Scenario**: GIVEN a valid JWT in Authorization header; WHEN GET /me is called; THEN 200 with JSON containing id, email, createdAt, displayName, bio, birthDate, and gender.

**Scenario**: GIVEN no JWT; WHEN GET /me is called; THEN 401 Unauthorized.
