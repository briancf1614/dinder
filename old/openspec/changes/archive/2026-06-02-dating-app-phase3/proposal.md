# Proposal: Dating App Phase 3 — Social, Safety & Analytics

## Intent

Phase 2 delivered the core loop (discovery, match, chat, subscriptions). Users can swipe and pay — but profiles are bare, moderation is manual, and we have zero business metrics. Phase 3 tackles engagement (social depth), trust (automated safety), and intelligence (analytics).

## Scope

### In Scope
- Profile prompts (Hinge-style Q&A, up to 3 per profile)
- Icebreaker questions (auto-assigned after mutual match)
- Automated AI photo moderation (NSFW detection via Azure AI Vision)
- Admin analytics dashboard (growth, conversion, retention, match rates)

### Out of Scope
- Kotlin mobile app, video chat, ML matching, gamification, events/speed dating

## Capabilities

### New Capabilities
- `profile-prompts`: Users add/edit up to 3 Hinge-style prompts; displayed on discovery cards and profile view
- `icebreaker-questions`: System assigns an icebreaker to each new match; shown in conversation header
- `ai-photo-moderation`: Azure AI Vision auto-reviews uploaded photos; flagged → manual queue; clean → auto-approved
- `analytics-metrics`: Admin dashboard shows user growth, subscription conversion, retention cohorts, match rate, swipe volume

### Modified Capabilities
- `user-profile`: UP-1 (Profile Creation & Editing) — now includes prompts alongside bio and photos
- `safety-moderation`: SM-3 (Photo Moderation Queue) — pipeline gains AI pre-screening; manual queue receives only flagged + appealed photos
- `admin-dashboard`: AD-2 scope expands with analytics views alongside existing report queue

## Approach

Extend existing bounded contexts — no new contexts. Social features add value objects to `Profile` and `Conversation` aggregates. AI moderation adds `MediaVerificationService` calling Azure AI Vision before the manual queue. Analytics uses MediatR fire-and-forget handlers writing to a new `analytics` PostgreSQL schema (no write-path blocking).

| Area | Impact | Description |
|------|--------|-------------|
| `user-profile` | Modified | `Profile` gains `Prompts` list; `CreateOrUpdateProfileCommand` extended |
| `real-time-chat` | Modified | `Conversation` gains `IcebreakerQuestion`; seeded on match creation |
| `discovery` | Modified | `CandidateDto` includes prompts; `MatchCreatedEvent` triggers icebreaker assignment |
| `safety-moderation` | Modified | `MediaVerificationService` calls Azure AI; `PhotoReview` auto-approves clean results |
| `media-storage` | Modified | Upload confirmation triggers AI moderation alongside existing pipeline |
| `admin-dashboard` | Modified | New `AnalyticsDbContext`, aggregation queries, chart endpoints |
| Angular frontend | Modified | Profile edit, discovery cards, icebreaker picker, admin charts |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| ~40% frontend effort (Angular stubs have no implementation) | High | Allocate 2 of ~5 sessions to frontend; build prompts UI first as template |
| AI moderation false positives | Medium | Retain manual override; rejected photos include appeal messaging; adjustable confidence threshold |
| Analytics write-path slows swipe/match flow | Low | Fire-and-forget MediatR handlers — proven pattern from Phase 2 |
| Azure AI Vision dependency (cost, availability) | Low | Config toggle falls back to manual-only queue; per-image cost ~$0.001 |

## Rollback Plan

Each feature is independently reversible:
- **Prompts/icebreakers**: Remove columns + migrations; UI hides the fields
- **AI moderation**: `UseAIModeration` config flag off; reverts to full manual queue
- **Analytics**: Drop `analytics` schema; remove dashboard routes; zero impact on core flow

## Dependencies

- Azure AI Vision (Computer Vision API v3.2) — REST SDK, no new infra
- Existing Azure Blob Storage and PostGIS (already configured)

## Success Criteria

- [ ] Users can add/edit up to 3 profile prompts; prompts display on discovery cards
- [ ] Icebreaker question appears in every new match conversation
- [ ] Uploaded photos auto-reviewed by AI; clean photos skip manual queue
- [ ] Admin dashboard shows: daily/weekly growth, conversion %, match rate, swipe volume
- [ ] All 141 existing tests still pass; new tests cover prompts, icebreakers, AI pipeline, analytics handlers
