# user-profiles Specification

## Purpose

Manage user dating profile data: display name, bio, birth date, and gender. Profile fields are optional at registration and settable via an authenticated endpoint.

## Requirements

### Requirement: Update Profile via PUT /me/profile

The system MUST accept profile updates from authenticated users via `PUT /me/profile`. The endpoint MUST persist all four profile fields to the User entity and return the full MeResponse (7 fields: Id, Email, CreatedAt, DisplayName, Bio, BirthDate, Gender).

#### Scenario: User sets complete profile

- GIVEN an authenticated user with a valid JWT
- WHEN PUT /me/profile is called with valid DisplayName, Bio, BirthDate (18+), and Gender
- THEN 200 OK with JSON containing all 7 MeResponse fields
- AND profile values are persisted to the Users table

#### Scenario: User sets partial profile

- GIVEN an authenticated user
- WHEN PUT /me/profile is called with only DisplayName (Bio, BirthDate, Gender omitted)
- THEN 200 OK — Bio, BirthDate, Gender are null
- AND DisplayName is persisted

#### Scenario: Unauthenticated request

- GIVEN no JWT in Authorization header
- WHEN PUT /me/profile is called
- THEN 401 Unauthorized

### Requirement: Profile Field Validation

The system MUST validate all profile fields before persistence and MUST reject invalid input with 400 Bad Request containing FluentValidation error messages.

#### Scenario: DisplayName required and length

- GIVEN an authenticated user
- WHEN PUT /me/profile is called with empty or whitespace-only DisplayName
- THEN 400 Bad Request with "Display name is required"
- WHEN DisplayName exceeds 100 characters
- THEN 400 Bad Request with "Display name must not exceed 100 characters"

#### Scenario: Bio character limit

- GIVEN an authenticated user
- WHEN PUT /me/profile is called with Bio exceeding 500 characters
- THEN 400 Bad Request with "Bio must not exceed 500 characters"

#### Scenario: BirthDate age and range

- GIVEN an authenticated user
- WHEN PUT /me/profile is called with BirthDate less than 18 years from today
- THEN 400 Bad Request with "You must be at least 18 years old"
- WHEN BirthDate is in the future
- THEN 400 Bad Request with "Birth date must be in the past"

#### Scenario: Invalid Gender value

- GIVEN an authenticated user
- WHEN PUT /me/profile is called with Gender not in {Male, Female, NonBinary, Other}
- THEN 400 Bad Request with "Gender must be a valid value"

### Requirement: GET /me Returns Profile Data

The system MUST include profile fields in GET /me responses for all authenticated users. Null profile fields MUST be returned as JSON null (not omitted from the response).

#### Scenario: User with complete profile

- GIVEN an authenticated user who has set all profile fields via PUT /me/profile
- WHEN GET /me is called
- THEN 200 OK with JSON containing id, email, createdAt, displayName, bio, birthDate, gender
- AND all values match what was previously persisted

#### Scenario: User with no profile set

- GIVEN an authenticated user who has never called PUT /me/profile
- WHEN GET /me is called
- THEN 200 OK — displayName, bio, birthDate, gender are all null
