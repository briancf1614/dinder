# Delta for User Profile

## ADDED Requirements

### Requirement: UP-6 Profile Prompts Integration

The profile MUST support up to 3 Hinge-style prompts selectable from a catalog. Prompt answers SHALL appear on public profiles and discovery cards. Prompts are optional — absence of prompts SHALL NOT block discovery.

#### Scenario: Profile includes prompts
- GIVEN a user with 3 prompts configured
- WHEN their profile is viewed or appears in discovery
- THEN prompts and answers display alongside bio and photos

#### Scenario: Empty prompts do not block discovery
- GIVEN a user with approved photo, bio, and preferences but 0 prompts
- WHEN `IsDiscoverable` is evaluated
- THEN the profile remains discoverable

## MODIFIED Requirements

### Requirement: UP-1: Profile Creation & Editing

The system MUST allow an authenticated user to create and edit a profile with display name, bio (max 500 chars), gender identity, interested-in preferences, and up to 3 prompt selections. A profile SHALL be marked `IsDiscoverable = false` until all required fields plus at least one approved photo are present.
(Previously: UP-1 had no prompt support — only display name, bio, gender, interested-in, birthday)

#### Scenario: Create profile with minimum fields
- GIVEN an authenticated user with no existing profile
- WHEN they submit display name, gender, interested-in, and birthday
- THEN a profile is created with `IsDiscoverable = false`
- AND the user is prompted to upload at least one photo and optionally add prompts

#### Scenario: Profile becomes discoverable
- GIVEN a profile has an approved photo, bio, and preferences
- WHEN the last required field is saved
- THEN `IsDiscoverable` is set to `true`

### Requirement: UP-2: Photo Management (up to 6)

The system MUST support uploading up to 6 photos via pre-signed URLs. Uploaded photos SHALL trigger an async AI moderation scan. Clean photos auto-approve; flagged photos enter the manual queue. At least one approved photo MUST exist for discovery.
(Previously: All photos entered full manual moderation queue — no AI pre-screening)

#### Scenario: Upload first photo
- GIVEN a user with zero photos
- WHEN they request a pre-signed upload URL, upload, and confirm
- THEN the photo is created with status `AIScanning`
- AND an async AI moderation scan is dispatched
- AND if clean, the photo auto-approves; if flagged, it enters the manual queue

#### Scenario: Exceed 6-photo limit
- GIVEN a user with 6 photos
- WHEN they attempt a 7th upload
- THEN the request is rejected with 422
