# Delta for Discovery

## ADDED Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| DI-6 | ML Scoring in Candidate Ranking | MUST |
| DI-7 | Daily Bonus Swipes for Streak | MUST |

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
