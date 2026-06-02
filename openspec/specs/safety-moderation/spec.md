# Safety & Moderation Specification

## Purpose

Protect users through report, block, and moderation mechanisms. Enable manual photo review and account banning to maintain platform safety — a non-negotiable baseline for a professional dating app.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| SM-1 | Report User (with reason and sub-category) | MUST |
| SM-2 | Block User (one-way, immediate) | MUST |
| SM-3 | Photo Moderation Queue (AI pre-screened) | MUST |
| SM-4 | Ban/Unban User (admin action) | MUST |
| SM-5 | Shadow-Ban (silent suspension) | MAY |
| SM-6 | Enhanced Report Sub-Categories | MUST |
| SM-7 | AI Moderation Integration | MUST |

### SM-1: Report User (with reason)

The system MUST allow any authenticated user to report another user with a required reason and optional sub-category. Reasons: Harassment, Fake Profile, Spam, Inappropriate Photos, Other. Repeated reports SHALL be allowed but deduplicated. All reports SHALL enter the admin review queue.

#### Scenario: Report a matched user for harassment

- GIVEN Alice and Bob are matched
- WHEN Alice reports Bob with reason "Harassment", sub-category "Verbal Abuse", and an optional description
- THEN a report is created and queued for admin review
- AND Alice receives a confirmation message

#### Scenario: Report a profile from discovery (no match)

- GIVEN Alice views Bob's profile in the discovery stack
- WHEN Alice reports Bob
- THEN the report is created successfully with reason and sub-category — no match requirement exists

### SM-2: Block User (one-way, immediate)

The system MUST support one-way blocking that takes immediate effect: the blocked user SHALL NOT see the blocker in discovery, send messages, or view their profile. Blocking SHALL NOT notify the blocked user. Unblocking SHALL restore discovery visibility but NOT restore prior conversations.

#### Scenario: Block an active match

- GIVEN Alice and Bob have an active conversation
- WHEN Alice blocks Bob
- THEN Bob is blocked immediately
- AND the conversation is hidden from Alice
- AND Bob cannot send messages to Alice
- AND Bob no longer appears in Alice's discovery

#### Scenario: Unblock does not restore conversation

- GIVEN Alice unblocks Bob
- WHEN Alice views her conversation list
- THEN the previous conversation with Bob is NOT restored

### SM-3: Photo Moderation Queue

The system MUST route all newly uploaded photos through AI pre-screening. Statuses: `AIScanning`, `PendingReview`, `Approved`, `Rejected`, `FlaggedByAI`. Clean photos auto-approve. Flagged photos enter the manual queue with AI confidence scores. Rejected users MUST be notified with a reason and MAY appeal.

#### Scenario: Photo enters AI scan on upload

- GIVEN a user uploads a new profile photo
- WHEN the upload confirmation is received
- THEN the photo status is set to `AIScanning`
- AND Azure AI Vision analyzes the photo
- AND if clean, the photo auto-approves; if flagged, it enters the manual queue with AI scores

#### Scenario: Admin approves photo

- GIVEN a photo in the queue with status `PendingReview` or `FlaggedByAI`
- WHEN an admin approves it
- THEN status changes to `Approved`
- AND the photo becomes visible on the user's public profile

#### Scenario: User appeals rejected photo

- GIVEN a photo was rejected (by AI or admin)
- WHEN the user submits an appeal with a reason
- THEN the photo re-enters the manual queue with status `Appealed`

### SM-4: Ban/Unban User (admin action)

The system MUST support banning a user from the admin dashboard. Banning SHALL immediately revoke all sessions, tokens, and SignalR connections. Unbanning SHALL restore access but NOT restore unmatched conversations or unblock previously blocked relationships.

#### Scenario: Admin bans a user

- GIVEN an admin bans user X with reason "Repeated harassment — 3rd report confirmed"
- WHEN the ban is executed
- THEN all active sessions and tokens for user X are revoked
- THEN user X's SignalR connections are terminated
- AND user X's profile is removed from the discovery pool
- AND the action is recorded in the audit log

### SM-5: Shadow-Ban (silent suspension)

The system MAY support shadow-banning: the user can still interact but their profile is hidden from discovery and their messages are silently dropped. This is a v2 feature, NOT required for Phase 1 MVP.

#### Scenario: Shadow-banned user invisible in discovery

- GIVEN user X is shadow-banned
- WHEN any other user requests their candidate queue
- THEN user X never appears in the results

### SM-6: Enhanced Report Sub-Categories

Reports MUST include a sub-category selection within each reason: Harassment (Verbal Abuse, Physical Threat, Stalking), Fake Profile (Catfish, Scam, Bot), Inappropriate Photos (Nudity, Violence, Spam Image). Sub-category filtering SHALL be available in the admin review queue.

#### Scenario: Sub-categorized harassment report

- GIVEN Alice reports Bob for Harassment
- WHEN Alice selects "Verbal Abuse" as sub-category
- THEN the report is filed with reason + sub-category

### SM-7: AI Moderation Integration

The moderation pipeline MUST integrate AI pre-screening via Azure AI Vision. Photos with AI scores below threshold SHALL auto-approve. Flagged photos SHALL enter the manual queue with visible AI scores. Rejected users MAY appeal, which re-enters the manual queue.
