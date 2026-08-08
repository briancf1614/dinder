# Delta for identity

## ADDED Requirements

### Requirement: User Entity Has Profile Columns

The User entity MUST include four new nullable columns: DisplayName (string, max 100), Bio (string, max 500), BirthDate (DateOnly), and Gender (enum: Male, Female, NonBinary, Other). These columns MUST default to null and remain null until explicitly set via PUT /me/profile.

#### Scenario: New user registration without profile

- GIVEN a user registers with email and password only
- WHEN the User entity is persisted
- THEN DisplayName, Bio, BirthDate, and Gender are null

## MODIFIED Requirements

### Requirement: GET /me Returns Authenticated User Info

The endpoint MUST be protected with `[Authorize]`. It MUST extract the email from the JWT and return the user's Id, Email, CreatedAt, DisplayName, Bio, BirthDate, and Gender. Profile fields that are null MUST be returned as JSON null.
(Previously: returned Id, Email, and CreatedAt only — 3 fields. Now returns 7 fields including profile data.)

#### Scenario: Authenticated request

- GIVEN a valid JWT in Authorization header
- WHEN GET /me is called
- THEN 200 with JSON containing id, email, createdAt, displayName, bio, birthDate, and gender

#### Scenario: Unauthenticated request

- GIVEN no JWT
- WHEN GET /me is called
- THEN 401 Unauthorized
