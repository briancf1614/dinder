# User Profile Specification

## Purpose

Manage user profile data — photos, bio, preferences, and geolocation. Profiles are the primary unit of discovery. A profile MUST be complete (at least one approved photo + preferences) to enter the discovery pool.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| UP-1 | Profile Creation & Editing | MUST |
| UP-2 | Photo Management (up to 6, moderation gated) | MUST |
| UP-3 | Preference Configuration | MUST |
| UP-4 | Geolocation Storage (PostGIS) | MUST |
| UP-5 | Age Gate — 18+ Only | MUST |

### UP-1: Profile Creation & Editing

The system MUST allow an authenticated user to create and edit a profile with display name, bio (max 500 chars), gender identity, and interested-in preferences. A profile SHALL be marked `IsDiscoverable = false` until all required fields plus at least one approved photo are present.

#### Scenario: Create profile with minimum fields

- GIVEN an authenticated user with no existing profile
- WHEN they submit display name, gender, interested-in, and birthday
- THEN a profile is created with `IsDiscoverable = false`
- AND the user is prompted to upload at least one photo

#### Scenario: Profile becomes discoverable

- GIVEN a profile has an approved photo, bio, and preferences
- WHEN the last required field is saved
- THEN `IsDiscoverable` is set to `true`
- AND the profile becomes eligible for candidate generation

### UP-2: Photo Management (up to 6)

The system MUST support uploading up to 6 photos via pre-signed URLs. Uploaded photos SHALL enter the moderation queue before becoming publicly visible. Users MAY reorder photos. At least one approved photo MUST exist to enable discovery.

#### Scenario: Upload first photo

- GIVEN a user with zero photos
- WHEN they request a pre-signed upload URL, upload, and confirm
- THEN the photo is created with status `PendingReview`
- AND the photo enters the moderation queue

#### Scenario: Exceed 6-photo limit

- GIVEN a user who already has 6 photos
- WHEN they attempt to request an upload URL for a 7th photo
- THEN the request is rejected with 422 Unprocessable Entity

### UP-3: Preference Configuration

The system MUST allow users to set discovery preferences: interested-in genders, age range (18–100), and max distance (1–500 km). Preferences MUST be saved before the user enters the discovery flow.

#### Scenario: Set discovery preferences

- GIVEN an authenticated user editing their profile
- WHEN they set interested-in to "Women", age range 25–40, max distance 50 km
- THEN preferences are persisted and immediately used by the candidate generator

### UP-4: Geolocation Storage (PostGIS)

The system MUST store user location as a PostGIS geography point (WGS84 SRID 4326). Location MUST be captured via browser geolocation API during profile creation and MAY be refreshed on each login. Raw coordinates SHALL NOT be exposed to other users.

#### Scenario: Capture location at profile creation

- GIVEN a user grants browser geolocation permission
- WHEN the profile creation form is submitted
- THEN latitude and longitude are stored as `geography(Point, 4326)`
- AND the profile is searchable by `ST_DWithin` proximity queries

### UP-5: Age Gate — 18+ Only

The system MUST validate that the user's birthday indicates age 18 or older at registration. Visitors under 18 MUST be rejected without creating any account or storing personal data.

#### Scenario: Underage visitor rejected

- GIVEN a visitor provides a birthday making them 16 years old
- WHEN they submit the registration form
- THEN registration is rejected with a clear age-requirement message
- AND no user account or personal data is persisted
