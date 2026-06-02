# Tasks: Dating App Phase 4 — Engagement & Intelligence

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~900 (20 files + 12 test scenarios) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (Foundation + Conversation List: ~350 lines) → PR 2 (Gamification + ML: ~450 lines) |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain foundation + conversation list endpoint | PR 1 | Base: feature/phase4-engagement. Entities, interfaces, enum, config, GET /api/v1/conversations, Angular wiring. |
| 2 | Gamification handlers + ML scoring | PR 2 | Base: PR 1 branch. Streak/achievement/reward handlers, ML.NET scorer, feature flag, SwipeCommand bonus logic. |

## Phase 1: Domain Foundation (PR 1)

- [x] 1.1 Add `DailyStreak` (int) + `LastStreakDate` (DateTime?) to `src/Dinder.Domain/Entities/User.cs`
- [x] 1.2 Create `src/Dinder.Domain/Enums/AchievementType.cs` — FirstMatch, CenturySwiper, ProfileComplete, SocialButterfly, StreakMaster
- [x] 1.3 Create `src/Dinder.Domain/Events/AchievementUnlockedEvent.cs` — `record(Guid UserId, AchievementType Type)`
- [x] 1.4 Create `src/Dinder.Domain/Interfaces/IProfileScorer.cs` — `ScoreAsync(Profile, List<Profile>, CancellationToken)`
- [x] 1.5 Create `src/Dinder.Domain/Interfaces/IAchievementRegistry.cs` — `AchievementDefinition GetDefinition(AchievementType)`
- [x] 1.6 Add `GetConversationsByUserIdAsync(Guid, Guid?, int, CancellationToken)` to `IChatRepository.cs`
- [x] 1.7 Create `src/Dinder.Application/Gamification/achievements.json` — 5+ badge definitions (name, description, iconKey, criteria)
- [x] 1.8 Update `src/Dinder.Infrastructure/Persistence/Configurations/UserConfiguration.cs` — EF mappings for new User columns

## Phase 2: Conversation List Endpoint (PR 1)

- [x] 2.1 Create `src/Dinder.Application/Chat/Queries/GetConversationsQuery.cs` with `ConversationDto` (id, displayName, lastMessage, unreadCount, icebreakerQuestion, icebreakerCategory) + cursor pagination
- [x] 2.2 Implement `GetConversationsByUserIdAsync` in `src/Dinder.Infrastructure/Persistence/ChatRepository.cs`
- [x] 2.3 Add `GET /api/v1/conversations` endpoint to `src/Dinder.Api/Controllers/ChatController.cs`
- [x] 2.4 Create `src/app/src/app/features/chat/chat.service.ts` — HTTP client for `getConversations(cursor?)`
- [x] 2.5 Wire icebreaker data from API response into `src/app/src/app/features/chat/conversation-header.component.ts`

## Phase 3: Conversation List Tests (PR 1)

- [x] 3.1 Unit test `GetConversationsQuery` — pagination, most-recent-first ordering, empty list, unmatched excluded (RC-6 scenarios)
- [x] 3.2 Integration test `GET /api/v1/conversations` — 200 with icebreaker data, 200 empty, 401/403

## Phase 4: Gamification Handlers (PR 2)

- [x] 4.1 Create `src/Dinder.Application/Gamification/Handlers/StreakHandler.cs` — `INotificationHandler<UserLoggedInEvent>`, UTC midnight boundary, action-gated increment/reset (GA-1)
- [x] 4.2 Create `src/Dinder.Application/Gamification/Handlers/AchievementHandler.cs` — handles `SwipeRecordedEvent` + `MatchCreatedEvent`, counter checks, fires `AchievementUnlockedEvent`, idempotency guard (GA-2)
- [x] 4.3 Create `src/Dinder.Application/Gamification/Handlers/DailyRewardHandler.cs` — streak milestones (7d=+5, 14d=+10, 30d=+15), premium stacking per `[RequiresTier]` (GA-4)

## Phase 5: ML Scoring Integration (PR 2)

- [x] 5.1 Create `src/Dinder.Infrastructure/Matching/MlNetProfileScorer.cs` — simple cosine similarity on prompts/interests/demographics, cold-start fallback (DI-6)
- [x] 5.2 Modify `src/Dinder.Application/Discovery/Queries/GetCandidatesQuery.cs` — inject `IProfileScorer`, gate behind `Matching:UseMLScoring` feature flag
- [x] 5.3 Modify `src/Dinder.Application/Discovery/Commands/SwipeCommand.cs` — query `User.DailyStreak` for bonus swipe calculation (DI-7)
- [x] 5.4 Register `MatchingFeatureFlags` singleton in `src/Dinder.Api/Program.cs` — default `UseMLScoring: false`

## Phase 6: Gamification + ML Tests (PR 2)

- [x] 6.1 Unit test `StreakHandler` — consecutive increment, missed-day reset, login-only no-op (GA-1)
- [x] 6.2 Unit test `AchievementHandler` — first-match unlock, 100-swipes milestone, idempotency (GA-2)
- [x] 6.3 Unit test `MlNetProfileScorer` — test vectors with known profiles, cold-start fallback (DI-6)
- [x] 6.4 Unit test `SwipeCommand` with bonus streaks — 7-day bonus accepted, 30-day cap, no-streak rejection (DI-7)
- [x] 6.5 Extend `EntitlementBehaviorTests` — verify 403 body includes `RequiredTier` + `CurrentTier` fields (EE-2, already implemented; test-only)
