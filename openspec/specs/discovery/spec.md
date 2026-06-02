# Discovery Specification

## Purpose

Generate candidate profiles based on user preferences, record swipe actions, detect mutual matches, and enforce daily swipe limits. This is the core matchmaking loop.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| DI-1 | Candidate Generation (filters + dedup) | MUST |
| DI-2 | Swipe Action Recording | MUST |
| DI-3 | Mutual Match Detection | MUST |
| DI-4 | Daily Swipe Limit (50 free) | MUST |
| DI-5 | Candidate Deduplication (session + permanent) | MUST |
| DI-6 | ML Scoring in Candidate Ranking | MUST |
| DI-7 | Daily Bonus Swipes for Streak | MUST |

### DI-1: Candidate Generation (filters + dedup)

The system MUST generate a candidate queue filtered by: interested-in genders, age range, and max distance (via `ST_DWithin`). Candidates SHALL exclude the user themselves, already-swiped profiles, and banned/shadow-banned users. Results SHALL be ordered by last-active recency.

#### Scenario: Generate candidates within filters

- GIVEN a user interested in Women, aged 25–40, within 50 km
- WHEN they request their candidate queue
- THEN only profiles matching all three criteria are returned
- AND the user's own profile is excluded
- AND every already-swiped profile is excluded

#### Scenario: Empty pool — no matching candidates

- GIVEN a user whose filters match zero profiles in the database
- WHEN they request their candidate queue
- THEN an empty list is returned with a "no more candidates nearby" message

### DI-2: Swipe Action Recording

The system MUST record each swipe as `{SwiperId, SwipedId, Direction, Timestamp}`. Swiping the same profile again MUST replace the previous swipe record (idempotent). The daily swipe counter SHALL increment for every swipe (right or left).

#### Scenario: Swipe right on a candidate

- GIVEN a user viewing candidate X
- WHEN they swipe right
- THEN a swipe record with direction `Right` is upserted
- AND the daily swipe count increments by 1
- AND candidate X is removed from the active queue

#### Scenario: Swipe left on a candidate

- GIVEN a user viewing candidate Y
- WHEN they swipe left
- THEN a swipe record with direction `Left` is upserted
- AND the daily swipe count increments by 1

### DI-3: Mutual Match Detection

The system MUST atomically detect a mutual match when User A swipes right on User B and User B has previously swiped right on User A. A Match record and a Conversation MUST be created within the same transaction. Both users SHALL be notified via domain event.

#### Scenario: Mutual match created

- GIVEN User B previously swiped right on User A
- WHEN User A swipes right on User B
- THEN a Match record is created atomically
- AND a Conversation is created for the match
- AND a `MatchCreated` domain event is published

#### Scenario: One-sided swipe — no match

- GIVEN User B either swiped left or has not yet swiped on User A
- WHEN User A swipes right on User B
- THEN no Match record or Conversation is created

### DI-4: Daily Swipe Limit (50 free)

The system MUST enforce a limit of 50 swipes per user per calendar day (UTC). Once reached, further swipes MUST be rejected. The counter SHALL reset at 00:00 UTC.

#### Scenario: Swipe limit reached

- GIVEN a user who has performed 50 swipes today
- WHEN they attempt a 51st swipe
- THEN the swipe is rejected with 429 Too Many Requests
- AND the response body includes the UTC reset time

#### Scenario: Limit resets at midnight UTC

- GIVEN the user from the previous scenario, now the next calendar day
- WHEN they attempt a swipe
- THEN the swipe is accepted and the daily counter starts at 1

### DI-5: Candidate Deduplication (session + permanent)

The system MUST never return the same candidate twice in a single session. Previously swiped profiles (from any session) SHALL be excluded permanently.

#### Scenario: No duplicate in session

- GIVEN a user has already seen candidate X in their current session
- WHEN the candidate queue is refreshed
- THEN candidate X does not appear in new results

### DI-6: ML Scoring in Candidate Ranking

The system MUST integrate a lightweight ML scoring layer into `GetCandidatesQuery` that ranks candidates by profile similarity (prompts, interests, demographic overlap). Scoring SHALL be gated behind a feature flag (`Matching:UseMLScoring`) defaulting to `false`. When disabled, the existing recency-based ordering SHALL remain unchanged. The scoring model SHALL use ML.NET for native C# inference with no external service dependency.

#### Scenario: ML scoring enabled — ranked candidates

- GIVEN `Matching:UseMLScoring` is `true`
- WHEN a user requests their candidate queue
- THEN candidates are ranked by similarity score descending (closest matches first)
- AND existing filter criteria (age, gender, distance, dedup) are still applied before scoring

#### Scenario: ML scoring disabled — baseline ordering

- GIVEN `Matching:UseMLScoring` is `false`
- WHEN a user requests their candidate queue
- THEN candidates are ordered by last-active recency (existing behavior unchanged)

#### Scenario: Cold start — new user with minimal data

- GIVEN a new user with only a profile photo and no prompts or interests
- WHEN ML scoring is enabled
- THEN the scorer falls back to demographic-only similarity (age proximity, location distance)

### DI-7: Daily Bonus Swipes for Streak

The system MUST grant bonus swipes to a user's daily limit based on their gamification streak. Bonus values SHALL be: 7-day streak = +5 swipes, 14-day = +10, 30-day = +15. The bonus SHALL add to the tier-based base limit. The system MUST query the Gamification context for the current streak value at swipe time.

#### Scenario: 7-day streak grants +5 bonus swipes

- GIVEN a Free-tier user (25 base swipes) with a 7-day streak
- WHEN they exhaust their 25 base swipes and attempt a 26th swipe
- THEN the swipe is accepted (5 bonus swipes available)
- AND the bonus counter decrements to 4 remaining

#### Scenario: No streak — no bonus swipes

- GIVEN a Free-tier user (25 base swipes) with a 0-day streak
- WHEN they exhaust their 25 base swipes and attempt a 26th
- THEN the swipe is rejected with 429 Too Many Requests

#### Scenario: Streak bonus caps at 30-day

- GIVEN a user with a 45-day streak (max bonus at 30-day = +15)
- WHEN their daily limit is calculated
- THEN only 15 bonus swipes are granted (not 45-day proportional)
