# Design: Dating App Phase 3 — Social, Safety & Analytics

## Technical Approach

Extend existing bounded contexts per the modular-monolith Clean Architecture established in Phase 1. No new contexts — social features add value objects to `Profile` and `Conversation` aggregates. AI moderation replaces the `PhotoUploadedModerationHandler` stub. Analytics follows the existing fire-and-forget MediatR notification pattern, writing to a new `analytics` PostgreSQL schema.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|----------|--------|-------------|-----------|
| **Prompt storage on Profile** | JSONB owned entity (max 3) on `profile.profiles` | Separate `profile_prompts` table | Tightly coupled to Profile lifecycle; max 3 items; avoids JOIN. EF Core owned-entity maps cleanly |
| **Prompt catalog** | `admin.prompt_catalog` table seeded via EF migration | Static JSON file only | Spec PP-4 requires admin management at runtime; seed file provides initial data |
| **Icebreaker assignment** | `MatchCreatedEvent` handler picks random weighted by category | Contextual selection, manual per-match | Spec IQ-1 mandates random; IQ-3 adds category weighting; reuse existing `MatchCreatedEvent` |
| **Icebreaker storage** | Columns on `communication.conversations` | Separate table | One question per conversation; denormalizing avoids JOIN in chat queries |
| **AI moderation integration** | Azure AI Vision REST v3.2 via `PhotoUploadedModerationHandler` (synchronous MediatR notification) | Azure AI Content Safety, async background job | Azure infra already configured; ~500ms latency acceptable post-upload; no new infra (channels/queues) |
| **AI scores location** | `PhotoReview` entity (`AdultScore`, `RacyScore`, `ViolenceScore` floats) | On `MediaFile` | Admin sees scores in moderation queue alongside photo; `PhotoReview` is the review record |
| **Photo status flow** | Extend `MediaStatus` enum: +`AIScanning`, +`FlaggedByAI` | Separate status tracking table | Minimal schema change; enum maps directly to SM-3 requirements |
| **Analytics data collection** | Domain events → MediatR notification handlers → `analytics.*` materialized tables | Direct DB writes in command handlers, separate ETL | Decouples write-path from analytics; proven fire-and-forget pattern from Phase 1/2; zero impact on swipe/match latency |
| **Analytics aggregation** | Handler upserts daily snapshot rows; API queries materialized data | Real-time aggregation on read, background job with Hangfire | Simpler: no scheduling infra; daily snapshots sufficient for admin dashboard |
| **Report sub-categories** | `string? SubCategory` on `Report` entity + `ReportSubCategory` enum | Separate table, free-text | Simple column addition; enum constrains values for filtering (AD-2, SM-6) |

## Data Flow

### AI Moderation Pipeline

```
User confirms upload ──► ConfirmUploadCommand ──► MediaFile(PendingReview) + PhotoReview
        │                                              │
        │                                   _mediator.Publish(PhotoUploadedEvent)
        │                                              │
        │                              PhotoUploadedModerationHandler
        │                                      │
        │                          MediaFile.Status = AIScanning
        │                                      │
        │                          Azure AI Vision REST call
        │                                 ╱           ╲
        │                     clean (<threshold)    flagged (≥threshold)
        │                           │                    │
        │              MediaFile.AutoApprove()    MediaFile.Status = FlaggedByAI
        │              PhotoReview.Approve(null)   PhotoReview.AIScores = {...}
        │                           │                    │
        ▼                           ▼                    ▼
  HTTP 201 response          Photo public           Manual queue (admin)
```

### Analytics Pipeline

```
SwipeCommand ──► SwipeRecordedEvent ──► TrackSwipeMetricsHandler ──► analytics.swipe_metrics (upsert)
LoginCommand ──► UserLoggedInEvent ──► TrackDAUHandler ──► analytics.daily_active_users (upsert)
StripeWebhook ──► SubscriptionActivatedEvent ──► TrackSubscriptionHandler ──► analytics.subscription_snapshots (upsert)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Domain/Entities/Profile.cs` | Modify | Add `_prompts` list (owned entity, JSONB), prompt CRUD methods |
| `Domain/Entities/Conversation.cs` | Modify | Add `IcebreakerQuestion`, `IcebreakerCategory` |
| `Domain/Entities/MediaFile.cs` | Modify | Add `SetAIScanning()`, `AutoApprove()` (nullable admin) |
| `Domain/Entities/PhotoReview.cs` | Modify | Add `AdultScore/RacyScore/ViolenceScore`, allow null admin on Approve |
| `Domain/Entities/Report.cs` | Modify | Add `SubCategory` string |
| `Domain/Entities/PromptCatalog.cs` | Create | Prompt catalog entity (text, category, isEnabled) |
| `Domain/Entities/IcebreakerLibrary.cs` | Create | Icebreaker question entity (text, category) |
| `Domain/Enums/MediaStatus.cs` | Modify | Add `AIScanning=3`, `FlaggedByAI=4` |
| `Domain/Enums/ReportSubCategory.cs` | Create | Harassment/FakeProfile/InappropriatePhotos sub-categories |
| `Domain/Events/SwipeRecordedEvent.cs` | Create | Published from SwipeCommand after save |
| `Domain/Events/SubscriptionActivatedEvent.cs` | Create | Published from Stripe webhook handler |
| `Domain/Events/UserLoggedInEvent.cs` | Create | Published from LoginCommand |
| `Domain/Interfaces/IAnalyticsRepository.cs` | Create | Upsert methods for analytics tables |
| `Domain/Interfaces/IAzureVisionService.cs` | Create | `AnalyzeImageAsync(blobKey) → AIScanResult` |
| `Application/Profile/Commands/UpdateProfilePromptsCommand.cs` | Create | Add/remove/reorder prompts (max 3, ≤150 chars) |
| `Application/Profile/Commands/CreateOrUpdateProfileCommand.cs` | Modify | Accept optional prompts in command |
| `Application/Media/Handlers/PhotoUploadedModerationHandler.cs` | Modify | Replace stub: call Azure AI, auto-approve or flag |
| `Application/Analytics/Handlers/TrackDAUHandler.cs` | Create | Subscribe to `UserLoggedInEvent`, upsert `analytics.daily_active_users` |
| `Application/Analytics/Handlers/TrackSwipeMetricsHandler.cs` | Create | Subscribe to `SwipeRecordedEvent`, upsert `analytics.swipe_metrics` |
| `Application/Analytics/Handlers/TrackSubscriptionHandler.cs` | Create | Subscribe to `SubscriptionActivatedEvent`, upsert `analytics.subscription_snapshots` |
| `Application/Analytics/Handlers/AssignIcebreakerHandler.cs` | Create | Subscribe to `MatchCreatedEvent`, assign random icebreaker |
| `Application/Admin/Queries/GetAnalyticsQuery.cs` | Create | DAU/WAU/MAU, conversion, match rate queries |
| `Application/Admin/Commands/ManagePromptCatalogCommand.cs` | Create | CRUD for prompt catalog |
| `Application/Admin/Commands/ManageIcebreakerLibraryCommand.cs` | Create | CRUD for icebreaker library |
| `Infrastructure/Persistence/AnalyticsDbContext.cs` | Create | New context, `analytics.*` schema |
| `Infrastructure/Persistence/AnalyticsRepository.cs` | Create | Implements `IAnalyticsRepository` |
| `Infrastructure/Storage/AzureVisionService.cs` | Create | Azure AI Vision REST client |
| `Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Modify | Register `AnalyticsDbContext`, `IAzureVisionService`, `IAnalyticsRepository` |
| `Api/Controllers/AdminController.cs` | Modify | Add analytics, prompt catalog, icebreaker endpoints |
| `Api/Program.cs` | Modify | Register `UseAIModeration` feature flag |

## REST Endpoints

### New Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `PUT` | `/api/v1/profile/prompts` | Required | Body: `{prompts: [{promptId, answer, order}]}` — replaces all prompts |
| `GET` | `/api/v1/profile/prompts/catalog` | Required | Returns available prompts (enabled, grouped by category) |
| `GET` | `/api/v1/discovery/candidates` | Modified | `CandidateDto` now includes `prompts[]` |
| `POST` | `/api/v1/admin/prompts` | Admin | Create prompt in catalog |
| `PUT` | `/api/v1/admin/prompts/{id}` | Admin | Update/enable/disable prompt |
| `POST` | `/api/v1/admin/icebreakers` | Admin | Create icebreaker question |
| `GET` | `/api/v1/admin/analytics/dau` | Admin | Query: `?days=30` → daily active user counts |
| `GET` | `/api/v1/admin/analytics/conversion` | Admin | Query: `?days=30` → subscription conversion % |
| `GET` | `/api/v1/admin/analytics/matches` | Admin | Query: `?days=30` → match rate + swipe-to-match ratio |
| `POST` | `/api/v1/admin/photos/{id}/override` | Admin | Human override AI decision (approve/reject flagged photo) |
| `POST` | `/api/v1/profile/photos/{id}/appeal` | Required | User appeals rejected photo → re-enters manual queue |

### Modified Endpoints

| Method | Route | Change |
|--------|-------|--------|
| `GET` | `/api/v1/admin/reports` | Add `?subCategory=` filter |
| `POST` | `/api/v1/moderation/report` | Accept `subCategory` in body |
| `GET` | `/api/v1/conversations` | Include `icebreakerQuestion` in response |
| `GET` | `/api/v1/discovery/candidates` | `CandidateDto` gains `prompts: [{text, answer}]` |

## Database Changes

| Schema | Table | Change |
|--------|-------|--------|
| `profile` | `profiles` | Add `prompts` JSONB column (array of `{PromptId, Answer, Order}`) |
| `admin` | `prompt_catalog` | **New**: `id`, `text`, `category`, `is_enabled`, `created_at` |
| `admin` | `icebreaker_library` | **New**: `id`, `text`, `category`, `is_enabled`, `created_at` |
| `communication` | `conversations` | Add `icebreaker_question`, `icebreaker_category` columns |
| `moderation` | `photo_reviews` | Add `adult_score`, `racy_score`, `violence_score` (nullable real) |
| `moderation` | `reports` | Add `sub_category` varchar |
| `analytics` | `daily_active_users` | **New**: `date`, `user_count` |
| `analytics` | `subscription_snapshots` | **New**: `date`, `tier`, `count` |
| `analytics` | `swipe_metrics` | **New**: `date`, `total_swipes`, `total_right_swipes`, `total_matches` |

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | Prompt validation (max 3, ≤150 chars), icebreaker random selection, AI score threshold logic | xUnit, mock `IAzureVisionService` |
| Integration | AI moderation end-to-end (handler calls real Azure emulator or recorded response), analytics upsert idempotency | Testcontainers PostgreSQL; `AnalyticsDbContext` |
| Contract | New admin analytics endpoints, prompt catalog CRUD | Swashbuckle OpenAPI validation |

## Migration / Rollout

- `UseAIModeration` feature flag (appsettings): `false` → all photos go to manual queue (current behavior)
- Prompt catalog seeded via EF migration; disabled prompts hidden from user-facing catalog
- Analytics schema is additive — dropping it has zero impact on core flow
- Rollback per feature: drop columns/tables, disable flags, revert migrations

## Open Questions

- [ ] Azure AI Vision API key provisioned? Which Azure subscription?
- [ ] AI confidence threshold default (proposal suggests adjustable — what's the starting value? 0.7?)
- [ ] Icebreaker category weighting: equal-weight all enabled categories, or configurable weights?
