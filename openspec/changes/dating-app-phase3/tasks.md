# Tasks: Dating App Phase 3 — Social, Safety & Analytics

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 1500–1700 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1: Domain+Infra (~380) → PR 2: Prompts+Icebreakers (~420) → PR 3: AI Moderation+Analytics (~480) → PR 4: Reports+Frontend+Tests (~420) |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain types, enums, events, interfaces, AnalyticsDbContext, AzureVisionService stub | PR 1 | Base: `feature/dating-app-phase3` |
| 2 | Profile prompts + icebreakers (CQRS, API, migrations, Angular prompts/discovery) | PR 2 | Base: PR 1 branch; PP-1/2/4, IQ-1/2/3, UP-1/6 |
| 3 | AI moderation + analytics (Azure handler, metrics handlers, admin API) | PR 3 | Base: PR 2 branch; AM-1/2/3/4, AN-1/2/3/5, SM-3/7, AD-5/6 |
| 4 | Reports sub-category, Angular admin dashboard, final tests | PR 4 | Base: PR 3 branch; SM-1/6, AD-2 |

## Phase 0: Domain Foundation & Infrastructure (PR 1)

- [x] 0.1 Add `AIScanning=3`, `FlaggedByAI=4` to `Domain/Enums/MediaStatus.cs` — AM-1, SM-3
- [x] 0.2 Create `Domain/Enums/ReportSubCategory.cs` — VerbalAbuse, PhysicalThreat, Stalking, Catfish, Scam, Bot, Nudity, Violence, SpamImage — SM-6
- [x] 0.3 Add `_prompts` owned-entity list (max 3, ≤150 chars) + CRUD to `Domain/Entities/Profile.cs` — PP-1, UP-6
- [x] 0.4 Add `IcebreakerQuestion`, `IcebreakerCategory` columns to `Domain/Entities/Conversation.cs` — IQ-1/2
- [x] 0.5 Add `SetAIScanning()`, `AutoApprove()` to `Domain/Entities/MediaFile.cs`; add `AdultScore/RacyScore/ViolenceScore` to `PhotoReview.cs` — AM-2/3
- [x] 0.6 Add `SubCategory` string to `Domain/Entities/Report.cs` — SM-1, SM-6
- [x] 0.7 Create `Domain/Entities/PromptCatalog.cs` and `Domain/Entities/IcebreakerLibrary.cs` — PP-4, IQ-3
- [x] 0.8 Create domain events: `SwipeRecordedEvent`, `SubscriptionActivatedEvent`, `UserLoggedInEvent` — AN-1/2/3
- [x] 0.9 Create `IAzureVisionService` (`AnalyzeImageAsync`) + `IAnalyticsRepository` interfaces — AM-1, AN-1
- [x] 0.10 Create `Infrastructure/Persistence/AnalyticsDbContext.cs` (schema `analytics`), `AnalyticsRepository.cs` — AN-1/2/3
- [x] 0.11 Create `Infrastructure/Storage/AzureVisionService.cs` stub, register all new services in DI — AM-1, AN-5

## Phase 1: Profile Prompts (PR 2)

- [ ] 1.1 Create `UpdateProfilePromptsCommand` + handler — replace all prompts (reject 4th with 422) — PP-1, UP-1
- [ ] 1.2 Modify `CreateOrUpdateProfileCommand` to accept optional `Prompts` — UP-6
- [ ] 1.3 Create `ManagePromptCatalogCommand` + handler for admin CRUD — PP-4
- [ ] 1.4 Add `PUT /profile/prompts`, `GET /profile/prompts/catalog` to ProfileController — PP-1/4
- [ ] 1.5 Add `POST/PUT /admin/prompts` to AdminController; seed catalog via EF migration — PP-4
- [ ] 1.6 Run EF migration: `prompts` JSONB on `profile.profiles`, `admin.prompt_catalog` table — PP-1/4
- [ ] 1.7 Modify `CandidateDto` to include `prompts: [{text, answer}]` — PP-2, UP-6

## Phase 2: Icebreaker Questions (PR 2)

- [ ] 2.1 Create `AssignIcebreakerHandler` — subscribe `MatchCreatedEvent`, random weighted by category, persist to Conversation — IQ-1/3
- [ ] 2.2 Create `ManageIcebreakerLibraryCommand` + handler for admin CRUD — IQ-3
- [ ] 2.3 EF migration: `icebreaker_question/category` on `communication.conversations`, `admin.icebreaker_library` table — IQ-1/3
- [ ] 2.4 Modify `GET /conversations` response to include `icebreakerQuestion` — IQ-2

## Phase 3: AI Photo Moderation (PR 3)

- [ ] 3.1 Implement `AzureVisionService.cs` — call Azure AI Vision REST v3.2, return `AIScanResult` — AM-1
- [ ] 3.2 Replace stub in `PhotoUploadedModerationHandler` — set AIScanning, call Azure, auto-approve below threshold, else FlaggedByAI — AM-2/3, SM-7
- [ ] 3.3 Add `POST /admin/photos/{id}/override` and `POST /profile/photos/{id}/appeal` endpoints — AM-4, SM-3
- [ ] 3.4 EF migration: `adult_score/racy_score/violence_score` on `moderation.photo_reviews` — AM-3
- [ ] 3.5 Add `UseAIModeration` feature flag in `Program.cs`; false → full manual queue — AM-5

## Phase 4: Analytics (PR 3)

- [ ] 4.1 Create `TrackDAUHandler` subscribing `UserLoggedInEvent` → upsert `analytics.daily_active_users` — AN-1
- [ ] 4.2 Create `TrackSwipeMetricsHandler` subscribing `SwipeRecordedEvent` → upsert `analytics.swipe_metrics` — AN-3
- [ ] 4.3 Create `TrackSubscriptionHandler` subscribing `SubscriptionActivatedEvent` → upsert `analytics.subscription_snapshots` — AN-2
- [ ] 4.4 Create `GetAnalyticsQuery` + handler — DAU, conversion%, match rate, swipe-to-match; support `?days=7/30/90` — AN-5
- [ ] 4.5 Add `GET /admin/analytics/dau|conversion|matches` to AdminController — AD-5
- [ ] 4.6 EF migration: `analytics.daily_active_users`, `subscription_snapshots`, `swipe_metrics` tables — AN-1/2/3

## Phase 5: Reports, Admin Views & Frontend (PR 4)

- [ ] 5.1 Modify `POST /moderation/report` to accept `SubCategory`; persist in handler — SM-1/6
- [ ] 5.2 Add `?subCategory=` filter to `GET /admin/reports`; display AI scores + `FlaggedByAI` filter in admin queue — AD-2/6
- [ ] 5.3 Angular: profile edit — prompt picker (catalog select, answer input, reorder) — PP-1/3
- [ ] 5.4 Angular: discovery cards render prompts; conversation header shows icebreaker — PP-2, IQ-2
- [ ] 5.5 Angular: admin dashboard — analytics charts (DAU line, conversion%, match rate) with time filters — AD-5
- [ ] 5.6 Angular: report form — sub-category picker within each reason — SM-6
- [ ] 5.7 Unit tests: prompt validation (max 3, char limit), icebreaker selection, AI threshold logic — PP-1, IQ-1, AM-2
- [ ] 5.8 Integration tests: AI moderation pipeline (mock Azure), analytics upsert idempotency — AM-1, AN-3
- [ ] 5.9 Verify all 141 existing tests pass; contract tests for new endpoints
