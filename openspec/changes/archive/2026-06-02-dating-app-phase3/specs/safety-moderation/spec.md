# Delta for Safety & Moderation

## ADDED Requirements

### Requirement: SM-6 Enhanced Report Sub-Categories

Reports MUST include a sub-category selection within each reason: Harassment (Verbal Abuse, Physical Threat, Stalking), Fake Profile (Catfish, Scam, Bot), Inappropriate Photos (Nudity, Violence, Spam Image). Sub-category filtering SHALL be available in the admin review queue.

#### Scenario: Sub-categorized harassment report
- GIVEN Alice reports Bob for Harassment
- WHEN Alice selects "Verbal Abuse" as sub-category
- THEN the report is filed with reason + sub-category

### Requirement: SM-7 AI Moderation Integration

The moderation pipeline MUST integrate AI pre-screening via Azure AI Vision. Photos with AI scores below threshold SHALL auto-approve. Flagged photos SHALL enter the manual queue with visible AI scores. Rejected users MAY appeal, which re-enters the manual queue.

## MODIFIED Requirements

### Requirement: SM-3: Photo Moderation Queue

The system MUST route all newly uploaded photos through AI pre-screening. Statuses: `AIScanning`, `PendingReview`, `Approved`, `Rejected`, `FlaggedByAI`. Clean photos auto-approve. Flagged photos enter the manual queue with AI confidence scores. Rejected users MUST be notified and MAY appeal.
(Previously: All photos entered a manual-only queue with statuses `PendingReview`, `Approved`, `Rejected`)

#### Scenario: Photo enters AI scan on upload
- GIVEN a user uploads a new profile photo
- WHEN the upload confirmation is received
- THEN the photo status is set to `AIScanning`
- AND Azure AI Vision analyzes the photo
- AND if clean, the photo auto-approves; if flagged, it enters the manual queue with AI scores

#### Scenario: Admin approves AI-flagged photo
- GIVEN a photo in the queue with status `FlaggedByAI` and AI scores displayed
- WHEN an admin approves it
- THEN status changes to `Approved` and the photo becomes public

#### Scenario: User appeals rejected photo
- GIVEN a photo was rejected (by AI or admin)
- WHEN the user submits an appeal with a reason
- THEN the photo re-enters the manual queue with status `Appealed`

### Requirement: SM-1: Report User (with reason)

The system MUST allow any authenticated user to report another user with a required reason and optional sub-category. Reasons: Harassment, Fake Profile, Spam, Inappropriate Photos, Other. Repeated reports SHALL be allowed but deduplicated.
(Previously: Reports had no sub-category field)

#### Scenario: Report with sub-category
- GIVEN Alice and Bob are matched
- WHEN Alice reports Bob with reason "Harassment" and sub-category "Verbal Abuse"
- THEN the report is queued for admin review with both fields
- AND Alice receives a confirmation

#### Scenario: Report from discovery (no match)
- GIVEN Alice views Bob's profile in discovery
- WHEN Alice reports Bob with sub-category "Catfish"
- THEN the report is created with reason + sub-category
