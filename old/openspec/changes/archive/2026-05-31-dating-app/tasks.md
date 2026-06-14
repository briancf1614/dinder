# Tasks: Dating App MVP — Phase 1 Core Loop

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 2500+ |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1: Infrastructure+Identity → PR 2: Profile+Discovery → PR 3: Chat+Notifications → PR 4: Moderation+Admin+Media |
| Delivery strategy | ask-on-risk |
| Chain strategy | feature-branch-chain |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Project scaffolding, DB, Identity | PR 1 | Base: feature/dating-app-mvp |
| 2 | Profile + Discovery | PR 2 | Base: PR 1 branch |
| 3 | Chat + Notifications | PR 3 | Base: PR 2 branch |
| 4 | Moderation + Admin + Media | PR 4 | Base: PR 3 branch |

## Phase 0: Project Scaffolding

- [x] 0.1 Create .NET solution with projects (Dinder.Api, Dinder.Application, Dinder.Domain, Dinder.Infrastructure, Dinder.Contracts) and Directory.Build.props (net10.0, nullable, implicit usings)
- [x] 0.2 Create docker-compose.yml with PostgreSQL+PostGIS, Azurite blob emulator, API service
- [x] 0.3 Scaffold Angular 20 standalone app with core/auth/features/shared structure and Signals setup
- [x] 0.4 Configure EF Core with Npgsql, create base DbContext with schema-per-context convention (`HasDefaultSchema`)
- [x] 0.5 Set up JWT auth infrastructure: token generation (15min access), validation middleware, refresh token rotation storage
- [x] 0.6 Add xUnit + Testcontainers project, Jasmine/Karma config, Swashbuckle for OpenAPI contract tests

## Phase 1: Identity & Access (IA-1..IA-6)

- [x] 1.1 Create `identity.users` schema+migration with email, password hash, external logins, ban status, soft-delete flag (IA-1, IA-6)
- [x] 1.2 Implement `POST /register` with email/password validation (≥8 chars, 1 upper, 1 digit), duplicate-email 409, verification-pending status (IA-1)
- [x] 1.3 Implement Google+Apple social login via `POST /login/external`, auto-create user+mapping on first sign-in (IA-2)
- [x] 1.4 Implement `POST /refresh` with rotation: issue new pair, invalidate previous, atomic reuse-detection triggers full revocation (IA-4)
- [x] 1.5 Implement `DELETE /account` with soft-delete immediate revocation, queue cascade to all 8 context schemas, 30-day retention (IA-5)
- [x] 1.6 Add token-revocation guard: banned/soft-deleted users rejected 403 on every request regardless of JWT expiry (IA-3, IA-6)

## Phase 2: User Profile (UP-1..UP-5)

- [x] 2.1 Create `profile.profiles` schema+migration with display name, bio (≤500), gender, interested-in, birthday, `IsDiscoverable`, PostGIS `geography(Point,4326)` location column + GiST index (UP-1, UP-4)
- [x] 2.2 Implement `GET/PUT /profile` with create-on-first-read, `IsDiscoverable` auto-set when bio+prefs+approved-photo present (UP-1)
- [x] 2.3 Implement browser geolocation capture at profile creation, store via `ST_GeomFromText`, never expose raw coords to other users (UP-4)
- [x] 2.4 Implement age-gate validation: reject registration if birthday indicates <18, no account persisted (UP-5)
- [x] 2.5 Implement `GET/PUT /profile/preferences` with interested-in genders, age range 18–100, max distance 1–500 km (UP-3)
- [x] 2.6 Implement photo ordering via `POST /profile/photos/reorder` and enforce ≤6 limit at upload-URL request time (UP-2)

## Phase 3: Discovery (DI-1..DI-5)

- [x] 3.1 Create `discovery.swipes` and `discovery.matches` schema+migration with upsert-capable swipe table and daily-count tracking (DI-2, DI-4)
- [x] 3.2 Implement `GET /discovery/candidates?cursor=` with `ST_DWithin` proximity filter, interested-in+age-range filter, exclude self+swiped+banned (DI-1, DI-5)
- [x] 3.3 Implement `POST /discovery/swipe` with idempotent upsert, daily count increment, 429 reject at 50/day with UTC-reset header (DI-2, DI-4)
- [x] 3.4 Implement atomic mutual-match detection: on right-swipe, check reverse right-swipe → create Match+Conversation in transaction, publish `MatchCreated` domain event (DI-3)
- [x] 3.5 Implement `GET /discovery/matches` returning active matches for current user

## Phase 4: Real-Time Chat (RC-1..RC-5)

- [x] 4.1 Create `communication.conversations` and `communication.messages` schema+migration with match-gated conversation creation (RC-1, RC-4)
- [x] 4.2 Implement `ChatHub` at `/hubs/chat` with JWT auth handshake, `SendMessage` (≤2000 chars, persist-before-ack), `TypingIndicator`, `MarkRead` (RC-1, RC-2)
- [x] 4.3 Implement `GET /conversations/{id}/messages?cursor=` with cursor-paginated ascending history, page size 50 (RC-5)
- [x] 4.4 Implement unmatch via `POST /conversations/{id}/unmatch`: hide conversation from both, retain messages for moderation, block new messages (RC-3)
- [x] 4.5 Add match-gated access guard: deny conversation access to non-participants and unmatched users with 403 (RC-4)

## Phase 5: Notifications (NF-1..NF-5)

- [x] 5.1 Create `notification.notifications` and `notification.device_tokens` schema+migration with per-type opt-out flags (NF-2, NF-3, NF-4)
- [x] 5.2 Implement `POST /notifications/register-token` with FCM/APNs token association, reassign on device-handover (NF-3)
- [x] 5.3 Implement MediatR handlers for `MatchCreated` and `MessageSent`: create notification records + dispatch push via FCM/APNs async, respect opt-out (NF-5, NF-1)
- [x] 5.4 Implement `NotificationHub` at `/hubs/notifications` with server→client `NewNotification` and `BadgeUpdate` (NF-2)
- [x] 5.5 Implement `GET /notifications?cursor=`, `POST /notifications/read` (single+batch), and `PUT /notifications/opt-out` (NF-2, NF-4)

## Phase 6: Safety & Moderation (SM-1..SM-4)

- [x] 6.1 Create `moderation.reports`, `moderation.blocks`, and `moderation.photo_reviews` schema+migration (SM-1, SM-2, SM-3)
- [x] 6.2 Implement `POST /moderation/report` with required reason enum (Harassment, Fake Profile, Spam, Inappropriate Photos, Other), dedup same reporter+target (SM-1)
- [x] 6.3 Implement `POST /moderation/block/{userId}` with immediate one-way block: hide from discovery, deny messages, no notification to blocked user (SM-2)
- [x] 6.4 Implement photo moderation queue: `PendingReview` on upload confirm, admin approve→`Approved` (visible), reject→notify uploader with reason (SM-3)
- [x] 6.5 Implement ban/unban: revoke all sessions+tokens+SignalR on ban, restore on unban, write append-only audit log entry (SM-4, AD-4)

## Phase 7: Admin Dashboard (AD-1..AD-4)

- [x] 7.1 Create `admin.audit_log` append-only schema+migration with admin ID, action type, target user ID, timestamp, reason (AD-4)
- [x] 7.2 Implement `GET /admin/users?q=` with email partial-match and ID exact-match, return account metadata + recent activity summary, paginated 50/page (AD-1)
- [x] 7.3 Implement `GET /admin/reports?status=` queue sorted oldest-first, filter by Pending/Resolved/Dismissed, `POST /admin/reports/{id}/resolve` with note+audit log (AD-2)
- [x] 7.4 Implement `POST /admin/users/{id}/ban` and `POST /admin/users/{id}/unban` with mandatory reason, audit log, immediate session+SignalR revocation (AD-3)

## Phase 8: Media Storage (MS-1..MS-4)

- [x] 8.1 Create `media.media_files` schema+migration with blob key, status (PendingReview/Approved/Rejected), owner FK (MS-2)
- [x] 8.2 Implement `POST /media/upload-url` generating Azure Blob SAS pre-signed PUT URL (10min expiry, 10MB max, jpeg/png/webp only), scoped to `users/{userId}/photos/{guid}.ext` (MS-1)
- [x] 8.3 Implement `POST /media/confirm` verifying blob existence via SDK, creating `MediaFile` with `PendingReview`, triggering moderation queue (MS-2)
- [x] 8.4 Implement CDN URL resolution for approved photos with `Cache-Control: public, max-age=86400` (MS-3)
- [x] 8.5 Implement GDPR cascade handler: delete all user blobs from storage and `MediaFile` records on account deletion (MS-4)
