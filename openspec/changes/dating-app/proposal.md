# Proposal: Dating App MVP — Phase 1 Core Loop

## Intent

Ship the core matching loop as a safety-complete, GDPR-ready web app. Not a prototype.

## Scope

### In Scope

| Module | Deliverable |
|--------|-------------|
| Auth | Email/Google/Apple Sign-In, JWT + refresh tokens, account deletion |
| Profile | Photos (≤6), bio, gender, preferences, geolocation |
| Discovery | Swipe stack, filters, mutual match, 50 swipes/day free |
| Chat | 1-on-1 real-time (SignalR), read receipts, unmatch |
| Notifications | Push (FCM+APNs), in-app center |
| Safety | Report, block, manual photo moderation queue |
| Admin | User lookup, report review, ban/unban |
| Infra | Modular monolith, PostgreSQL+PostGIS, Angular 20, Docker Compose |

### Out of Scope

Payments (Phase 2), native mobile (Phase 3), ML matching, video chat, social graph, automated NSFW.

## Capabilities

### New (all greenfield)

- `identity-access`: Registration, social login, JWT, GDPR deletion
- `user-profile`: Photos, bio, preferences, geolocation
- `discovery`: Candidate generation, swipe events, mutual match
- `real-time-chat`: SignalR messaging, read receipts, unmatch
- `notifications`: Push, in-app center, device tokens
- `safety-moderation`: Reports, blocks, photo queue, bans
- `admin-dashboard`: User search, moderation actions
- `media-storage`: Pre-signed uploads, Azure Blob, CDN

### Modified

None.

## Approach

Modular monolith (Clean Architecture) with vertical slices per bounded context. PostgreSQL with per-context schemas. SignalR in-process. MediatR domain events. Angular 20 + Signals. REST + OpenAPI for CRUD; SignalR for real-time.

## Affected Areas

| Area | Impact |
|------|--------|
| `src/Dinder.Api/` | New — host, controllers |
| `src/Dinder.Application/` | New — CQRS, use cases |
| `src/Dinder.Domain/` | New — entities, aggregates |
| `src/Dinder.Infrastructure/` | New — EF Core, SignalR, storage |
| `src/Dinder.Modules/` | New — 6 vertical slices |
| `src/app/` | New — Angular 20 |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| PostGIS query perf at scale | Medium | GiST indexes; benchmark 100K profiles |
| SignalR + JWT refresh mid-connection | Medium | Prototype reconnect lifecycle |
| Photo moderation pipeline latency | High | Manual queue; async pipeline |
| GDPR cascade deletion gaps | High | Soft+hard delete cascade, day one |
| Token refresh race conditions | Medium | Atomic invalidation; queue refresh |

## Rollback Plan

Feature flags per module. DB DOWN migrations. Blob versioning for photos.

## Dependencies

Docker, .NET 10 SDK, Node 24, Angular CLI 20, PostgreSQL 16+ (PostGIS), Azure.

## Success Criteria

- [ ] Register → profile → browse candidates <5 min
- [ ] Mutual match enables chat <2 sec
- [ ] Report/block takes immediate effect
- [ ] Account deletion cascades all data (GDPR)
- [ ] Admin search + moderation functional
