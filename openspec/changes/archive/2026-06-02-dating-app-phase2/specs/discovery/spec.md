# Delta for Discovery

## MODIFIED Requirements

### DI-4: Daily Swipe Limit (tier-aware)

The system MUST enforce a per-tier daily swipe limit: **Free**: 25 swipes/day, **Plus**: 100 swipes/day, **Premium**: unlimited. The tier is read from JWT claims for fast gate evaluation. The counter SHALL reset at 00:00 UTC. Free and Plus users who reach their limit MUST be rejected with 429 and offered an upgrade path.

(Previously: Hardcoded 50-swipe/day limit for all users with no tier awareness.)

#### Scenario: Free user hits swipe limit

- GIVEN a Free-tier user who has performed 25 swipes today
- WHEN they attempt a 26th swipe
- THEN the swipe is rejected with 429 Too Many Requests
- AND the response body includes `upgrade_url` pointing to the Plus checkout

#### Scenario: Plus user within limit — accepted

- GIVEN a Plus-tier user who has performed 99 swipes today
- WHEN they attempt a 100th swipe
- THEN the swipe is accepted

#### Scenario: Premium user — unlimited

- GIVEN a Premium-tier user who has performed 500 swipes today
- WHEN they attempt a 501st swipe
- THEN the swipe is accepted

#### Scenario: Limit resets at midnight UTC

- GIVEN a Free-tier user who hit their limit yesterday
- WHEN they attempt a swipe on the next calendar day
- THEN the swipe is accepted and the daily counter starts at 1
