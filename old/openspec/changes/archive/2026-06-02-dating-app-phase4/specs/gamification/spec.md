# Gamification Specification

## Purpose

Drive daily retention through streaks, achievements, profile completeness scoring, and daily reward bonuses. All mechanics are additive — no breaking changes to core swipe→match→chat loop.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| GA-1 | Daily Login Streaks | MUST |
| GA-2 | Achievement Badge System | MUST |
| GA-3 | Profile Completeness Score | MUST |
| GA-4 | Daily Swipe Bonuses for Streaks | MUST |
| GA-5 | Achievement Push Notifications | MUST |

### GA-1: Daily Login Streaks

The system MUST track consecutive login days per user using UTC midnight boundaries. A login SHALL count toward the streak only when accompanied by a meaningful action (swipe or message sent) on that UTC day. The streak counter SHALL reset to 0 on a missed day.

#### Scenario: Consecutive login extends streak

- GIVEN a user with a 3-day streak who logs in and performs at least one swipe on the current UTC day
- WHEN the new UTC day begins and they log in with a swipe
- THEN the streak counter increments to 4

#### Scenario: Missed day resets streak

- GIVEN a user with a 5-day streak who last logged in 48+ hours ago
- WHEN they log in and perform a swipe
- THEN the streak counter resets to 1

#### Scenario: Login-only (no action) does not count

- GIVEN a user who logs in but performs no swipes or messages on that UTC day
- WHEN the UTC day ends
- THEN the streak does NOT increment for that day

### GA-2: Achievement Badge System

The system MUST define achievements as data-driven definitions (not hardcoded). Achievements SHALL be unlocked via domain event handlers listening to existing events (`SwipeRecordedEvent`, `MatchCreatedEvent`, `UserLoggedInEvent`). Each achievement SHALL have a name, description, icon key, and unlock criteria. The system MUST fire a domain event (`AchievementUnlockedEvent`) on unlock.

#### Scenario: First match unlocks achievement

- GIVEN a user with zero matches who has not yet earned the "First Match" badge
- WHEN a `MatchCreatedEvent` fires for this user
- THEN the "First Match" achievement is unlocked
- AND an `AchievementUnlockedEvent` is published

#### Scenario: 100 swipes unlocks milestone badge

- GIVEN a user with 99 lifetime swipes
- WHEN the 100th `SwipeRecordedEvent` fires
- THEN the "Century Swiper" achievement is unlocked

#### Scenario: Already-unlocked achievement is idempotent

- GIVEN a user who already holds the "Profile Complete" badge
- WHEN the profile completeness event fires again (e.g. re-save)
- THEN the achievement is NOT re-awarded (no-op)

### GA-3: Profile Completeness Score

The system MUST compute a profile completeness percentage (0–100%) based on: photo uploaded, bio filled, preferences set, and at least one prompt answered. The score SHALL be visible in the profile UI.

#### Scenario: Partial profile

- GIVEN a user with a photo and bio but no preferences or prompts
- WHEN their profile score is computed
- THEN the score is 50%

#### Scenario: Fully complete profile

- GIVEN a user with photo, bio, preferences, and at least one prompt
- WHEN their profile score is computed
- THEN the score is 100%
- AND the "Profile Complete" achievement is evaluated

### GA-4: Daily Swipe Bonuses for Streaks

The system MUST grant bonus swipes at streak milestones (7-day: +5, 14-day: +10, 30-day: +15). Bonus swipes SHALL stack on top of the tier-based daily limit. The `[RequiresTier]` attribute for premium bonus stacking MUST be honored.

#### Scenario: 7-day streak grants bonus swipes

- GIVEN a Free-tier user with a 7-day streak and 25 base swipes used
- WHEN they perform their 26th swipe of the day
- THEN the swipe is accepted (5 bonus swipes added to the daily limit)

#### Scenario: Premium user stacks bonuses

- GIVEN a Premium user with a 7-day streak (unlimited base + 5 bonus)
- WHEN they swipe
- THEN the bonus count is tracked but does not gate swipes (unlimited tier unaffected)

### GA-5: Achievement Push Notifications

The system MUST push achievement unlocks to connected clients via the existing NotificationHub. The push SHALL include the achievement name, icon key, and unlock timestamp.

#### Scenario: Achievement push delivered to online user

- GIVEN a user connected via SignalR who just unlocked the "First Match" achievement
- WHEN the `AchievementUnlockedEvent` is handled
- THEN the NotificationHub delivers the achievement details to the user
