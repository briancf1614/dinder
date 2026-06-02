# Design: Dating App Phase 4 — Engagement & Intelligence

## Technical Approach

Phase 4 adds a new `gamification` bounded context (schema-isolated PostgreSQL) with domain event handlers listening to existing `SwipeRecordedEvent`, `MatchCreatedEvent`, `UserLoggedInEvent`. ML scoring wraps ML.NET into a `ProfileSimilarityScorer` injected into `GetCandidatesQuery` behind a feature flag. The missing `GET /api/v1/conversations` fills the Phase 3 gap. Entitlement is already correct — the 403 body already includes `requiredTier` + `currentTier` (see `ForbiddenExceptionMiddleware` line 38-39); the spec's "MODIFIED" requirement is met.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|----------|--------|--------------|-----------|
| Gamification storage | New `gamification` schema in PostgreSQL — `GamificationDbContext` + EF Core | Add columns to `User` table in identity schema | Isolates gamification concerns, avoids polluting identity `DinderDbContext`. Follows existing pattern (analytics, moderation, admin each have own schema). |
| Streak as value object on User | Column `DailyStreak` + `LastStreakDate` on `User` entity in identity schema | Separate `UserStreak` entity in gamification schema | Streak is a user attribute queried at login/swipe time; co-locating avoids cross-context DB round-trip for every swipe-limit check. |
| Achievement definitions | JSON file `achievements.json` loaded at startup into singleton `IAchievementRegistry` | DB table `Achievements` with EF Core migration | Data-driven but simpler than full migration — achievements change rarely. Can migrate to DB later. |
| ML scorer integration | `IProfileScorer` interface → `MlNetProfileScorer` (ML.NET) inserted into `GetCandidatesQueryHandler` | Call external Python service | ML.NET is native C#, no infra overhead, fits existing DI. |
| A/B toggle | `MatchingFeatureFlags` singleton (follows `SubscriptionFeatureFlags` pattern) | `IConfiguration` read per-request | Singleton matches existing pattern; toggle cached at startup. |
| Conversation list | New `GetConversationsQuery` in `Dinder.Application.Chat.Queries`, controller method on `ChatController` | Separate controller | Single controller per domain is established pattern. |

## Data Flow

```
Login ──→ UserLoggedInEvent ──→ StreakHandler (gamification)
                                   ├─ updates User.DailyStreak
                                   └─ evaluates streak milestones → bonus swipes

Swipe ──→ SwipeRecordedEvent ──→ AchievementHandler (gamification)
                                   └─ checks counters → unlocks Achievement → AchievementUnlockedEvent
                                                                                  └─ NotificationHub push

GetCandidates ──→ GetCandidatesQueryHandler
                     ├─ [UseMLScoring=true] → IProfileScorer.Score(profile, candidates) → rank
                     └─ [UseMLScoring=false] → baseline recency order (unchanged)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Dinder.Domain/Entities/User.cs` | Modify | Add `DailyStreak`, `LastStreakDate` |
| `src/Dinder.Domain/Enums/` | Create | `AchievementType.cs` enum (FirstMatch, CenturySwiper, ProfileComplete, etc.) |
| `src/Dinder.Domain/Events/AchievementUnlockedEvent.cs` | Create | `record(Guid UserId, AchievementType Type)` |
| `src/Dinder.Domain/Interfaces/IProfileScorer.cs` | Create | `Task<IReadOnlyList<ProfileScore>> ScoreProfilesAsync(Profile, List<Profile>)` |
| `src/Dinder.Domain/Interfaces/IAchievementRegistry.cs` | Create | Read-only achievement definitions |
| `src/Dinder.Application/Gamification/Handlers/StreakHandler.cs` | Create | `INotificationHandler<UserLoggedInEvent>` — streak logic |
| `src/Dinder.Application/Gamification/Handlers/AchievementHandler.cs` | Create | Handles `SwipeRecordedEvent`, `MatchCreatedEvent` |
| `src/Dinder.Application/Gamification/Handlers/DailyRewardHandler.cs` | Create | Streak milestone → bonus swipe calculation |
| `src/Dinder.Application/Gamification/Entities/` | Create | `Achievement`, `UserAchievement` (if DB-backed later; MVP uses in-memory registry) |
| `src/Dinder.Application/Gamification/achievements.json` | Create | Badge definitions (5+ achievements) |
| `src/Dinder.Application/Chat/Queries/GetConversationsQuery.cs` | Create | `GetConversationsByUserIdAsync` → paged result |
| `src/Dinder.Application/Discovery/Queries/GetCandidatesQuery.cs` | Modify | Inject `IProfileScorer`, feature flag gating |
| `src/Dinder.Application/Discovery/Commands/SwipeCommand.cs` | Modify | Query `User.DailyStreak` for bonus swipe calculation |
| `src/Dinder.Infrastructure/Matching/MlNetProfileScorer.cs` | Create | ML.NET similarity via OneHotEncoding + cosine |
| `src/Dinder.Infrastructure/Persistence/Configurations/UserConfiguration.cs` | Modify | Add `DailyStreak`, `LastStreakDate` columns |
| `src/Dinder.Infrastructure/Persistence/ChatRepository.cs` | Modify | Add `GetConversationsByUserIdAsync` |
| `src/Dinder.Domain/Interfaces/IChatRepository.cs` | Modify | Add `GetConversationsByUserIdAsync` signature |
| `src/Dinder.Api/Controllers/ChatController.cs` | Modify | Add `GET /api/v1/conversations` endpoint |
| `src/Dinder.Api/Program.cs` | Modify | Add `MatchingFeatureFlags` singleton |
| `src/app/src/app/features/chat/conversation-header.component.ts` | Modify | Wire icebreaker data from API response |
| `src/app/src/app/features/chat/` | Create | `chat.service.ts` (HTTP client for conversation list) |

## Interfaces / Contracts

```csharp
// New repository method on IChatRepository
Task<List<Conversation>> GetConversationsByUserIdAsync(
    Guid userId, Guid? cursor, int limit, CancellationToken ct);

// Query result DTO
public sealed record ConversationsResult(
    List<ConversationDto> Conversations, Guid? NextCursor);
public sealed record ConversationDto(
    Guid ConversationId, string DisplayName, string? LastMessage,
    int UnreadCount, string? IcebreakerQuestion, string? IcebreakerCategory);

// ML scorer
public interface IProfileScorer
{
    Task<IReadOnlyList<ScoredProfile>> ScoreAsync(Profile user, List<Profile> candidates, CancellationToken ct);
}
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | Streak logic (UTC boundary, reset, action-gated) | xUnit — pure domain logic |
| Unit | EntitlementBehavior tier metadata in ForbiddenException | Extend existing `EntitlementBehaviorTests` |
| Unit | GetConversationsQuery paging + filtering | Moq repositories |
| Unit | MlNetProfileScorer similarity computation | Test vectors with known profiles |
| Integration | SwipeCommand with bonus streaks | Test DB with seeded User + Swipe data |
| Integration | GET /api/v1/conversations 200/403/empty | WebApplicationFactory |

## Migration / Rollout

All additive. New gamification schema migration via `GamificationDbContext` + EF Core tooling. `Matching:UseMLScoring` defaults to `false`. Streak/achievement handlers fire-and-forget — disable via DI if needed. No data migration required.

## Open Questions

- [ ] `Matching:UseMLScoring` config key — should it match the new `MatchingFeatureFlags` pattern or use plain `IConfiguration`?
- [ ] Gamification schema name — `gamification` or fold into existing `analytics` schema?
