# Proposal: Dating App Phase 4 — Engagement & Intelligence

## Intent

Phase 3 delivered social depth (prompts, icebreakers), safety (AI moderation), and analytics. Phase 4 shifts to **retention mechanics** — gamification drives DAU, ML scoring improves match quality, and known Phase 3 debt is repaid. All work is additive: no breaking changes to the core swipe→match→chat loop.

## Scope

### In Scope
- Fix missing `GET /api/v1/conversations` endpoint (Phase 3 warning) and wire icebreaker data to Angular
- Gamification: daily login streaks, achievement badges, profile completeness score, daily rewards
- ML matching groundwork: profile similarity scorer with A/B toggle, ML.NET scaffold

### Out of Scope
- Video Chat (WebRTC + TURN infra) → deferred to Phase 6+
- Virtual Speed Dating → deferred to Phase 6+
- Kotlin Mobile → deferred to Phase 5 (needs dedicated toolchain phase)
- Full ML model training pipeline → groundwork only (lightweight scoring, no training)

## Capabilities

### New Capabilities
- `gamification`: streaks, achievements, profile completeness score, daily swipe bonuses, achievement push via NotificationHub

### Modified Capabilities
- `real-time-chat`: add conversation list query + endpoint, wire icebreaker data to Angular header component
- `discovery`: add ML scoring layer (profile similarity + preference weights) behind configurable A/B toggle
- `entitlement-enforcement`: polish 403 response body (tier requirement message)

## Approach

**Fix Known Issues (P0, 1-2 tasks)**: Add `GetConversationsByUserIdAsync` to `IChatRepository`, wire `GetConversationsQuery` + `GET /api/v1/conversations`, connect Angular `conversation-header.component` icebreaker input to API.

**Gamification (P1, ~7 tasks)**: New `Gamification` bounded context with entities (Streak, Achievement, UserAchievement, DailyReward). Domain event handlers listen to existing `UserLoggedInEvent`, `SwipeRecordedEvent`, `MatchCreatedEvent` from Analytics pipeline. Streak calculation at UTC midnight boundary. Achievement definitions stored data-driven (not hardcoded). Daily rewards integrated with `[RequiresTier]` for premium bonuses. NotificationHub pushes achievement unlocks.

**ML Matching (P1, ~3 tasks)**: ML.NET NuGet for native C# scoring. Profile similarity computed from prompts, interests, and demographic overlap. Weighted ranking inserted into `GetCandidatesQuery` behind feature flag (`Matching:UseMLScoring`). A/B toggle allows comparison against baseline filter-only candidates.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Gamification anti-gaming (login-only streaks) | Medium | Require meaningful action (swipe/message) for streak credit |
| ML cold start (new users, no behavioral data) | Low | Fall back to profile similarity before behavioral signals |
| 400-line review budget exceeded | Medium | Auto-chain into 2 PRs: PR1 (fix + entities) → PR2 (handlers + Angular + ML) |

## Rollback Plan

All features are additive with feature flags. Gamification handlers are fire-and-forget (can be disabled at DI registration). ML scoring toggle (`Matching:UseMLScoring`) defaults to `false`. Conversation list endpoint is a new route — zero impact on existing endpoints.

## Dependencies

- ML.NET 4.0 NuGet (`Microsoft.ML`)
- Existing domain event pipeline (MediatR `INotificationHandler<T>`)
- Existing NotificationHub for achievement push

## Success Criteria

- [ ] `GET /api/v1/conversations` returns paginated list with icebreaker data and unread counts
- [ ] Streak counter increments on daily login with swipe activity
- [ ] 5+ achievements unlockable (profile complete, first match, 100 swipes, etc.)
- [ ] Profile completeness score visible in profile UI (0-100%)
- [ ] ML scoring pipeline produces ranked candidates behind `UseMLScoring` flag
- [ ] 0 build errors, all existing 184 tests pass, 35-40 new tests
