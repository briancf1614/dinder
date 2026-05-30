# Design: Dating App MVP — Phase 1 Core Loop

## Technical Approach

Modular monolith (Clean Architecture) with per-bounded-context vertical slices. ASP.NET Core hosts REST + SignalR in a single process (Docker). Cross-context communication via MediatR domain events — extractable to message bus later. PostgreSQL with one schema per context. Angular 20 standalone components with Signals for state. Pre-signed URLs for photo uploads (client→blob direct). All 8 specs map to 8 modules: Identity, Profile, Discovery, Communication, Notification, Moderation, Admin, Media.

## Architecture Decisions

| Decision | Choice | Alternatives Rejected | Rationale |
|----------|--------|----------------------|-----------|
| **Overall architecture** | Modular monolith | Microservices | 2-person team; no scale problems yet; each module folder is a future extraction point |
| **Database schema strategy** | Per-context PostgreSQL schemas (`identity.*`, `profile.*`, etc.) | Single `public` schema, separate DBs | Logical separation without operational overhead; EF Core `HasDefaultSchema()` per context |
| **Cross-context communication** | MediatR `INotification` in-process | gRPC, RabbitMQ | Zero infra; handlers can become queue consumers later; `MatchCreated → Notification` already spec'd |
| **Real-time transport** | SignalR (in-process) | WebSocket raw, Socket.IO | Built into ASP.NET Core; Azure SignalR Service for scale-out; JWT auth in handshake |
| **Frontend state** | Angular Signals + services | NgRx Store | Adequate for MVP complexity; no reducer boilerplate; services with `computed()` / `effect()` |
| **File upload pattern** | Pre-signed URL (client→Azure Blob) | Multipart through API | Zero API bandwidth for uploads; CDN direct delivery; moderation pipeline triggered on confirmation |
| **Pagination** | Cursor-based (keyset) | Offset-based | Stable under concurrent inserts; discovery and message history are append-heavy |
| **Token strategy** | JWT access (15 min) + rotating refresh (30 days) | Long-lived JWT only, Opaque tokens | Access revoked by ban immediately (expiry check + DB lookup); refresh rotation detects theft |
| **GDPR deletion** | Soft-delete → 30-day hard-delete cascade | Immediate hard-delete | Grace period for accidental deletion; cascade touches all 8 context schemas |

## Data Flow

```
                  ┌──────────────┐
                  │   Angular 20  │──REST/OpenAPI──┐
                  │ (PWA client)  │──SignalR WS────┤
                  └──────────────┘                 │
                                                   ▼
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────────────┐
│ Identity │◄─│ Profile  │◄─│Discovery │◄─│  Communication   │
│ (auth)   │  │(bio+geo) │  │(swipes)  │  │  (SignalR chat)  │
└────┬─────┘  └────┬─────┘  └────┬─────┘  └────────┬────────┘
     │              │             │                  │
     └──────────────┼─────────────┼──────────────────┘
                    │             │
                    ▼             ▼
          ┌────────────────────────────┐
          │     Notification Context    │
          │  (MediatR: MatchCreated,    │
          │   MessageSent → push+IAC)   │
          └────────────────────────────┘
                    │
     ┌──────────────┼──────────────┐
     ▼              ▼              ▼
┌──────────┐  ┌──────────┐  ┌──────────┐
│Moderation│  │  Admin   │  │  Media   │
│(reports, │  │(lookup,  │  │(Blob+CDN)│
│ blocks)  │  │ ban queue)│  │          │
└──────────┘  └──────────┘  └──────────┘
```

Core loop: Register → Profile → Candidates → SwipeRight → MatchCreated → Conversation created → Messages (SignalR) → Notifications pushed. Moderation reads Profile/Communication. Admin reads all, writes bans to Identity. Media serves Profile and Communication.

## File Changes

| Path | Action | Description |
|------|--------|-------------|
| `src/Dinder.Api/` | Create | Host, controllers, middleware, SignalR Hubs, Program.cs |
| `src/Dinder.Application/` | Create | CQRS handlers, DTOs, MediatR behaviors, validators |
| `src/Dinder.Domain/` | Create | Entities, value objects, domain events, repository interfaces |
| `src/Dinder.Infrastructure/` | Create | EF Core DbContexts, migrations, SignalR hubs, Azure Blob client, JWT provider |
| `src/Dinder.Infrastructure/Persistence/Configurations/` | Create | Per-entity EF Core configuration (8 contexts) |
| `src/Dinder.Contracts/` | Create | Shared DTOs, hub method contracts (reused by Angular via OpenAPI gen) |
| `src/app/` | Create | Angular 20 app: `core/` (auth, signalr, http), `features/` (onboarding, discovery, chat, profile, settings, admin), `shared/` |
| `docker-compose.yml` | Create | Services: api, db (PostgreSQL+PostGIS), azurite (dev blob emulator) |
| `Directory.Build.props` | Create | Common .NET settings (nullable, implicit usings, target `net10.0`) |

All files are NEW — greenfield project.

## Interfaces / Contracts

**REST endpoints** (per bounded context, versioned `/api/v1/`):

| Context | Key Endpoints |
|---------|--------------|
| Identity | `POST /register`, `POST /login`, `POST /refresh`, `DELETE /account` |
| Profile | `GET/PUT /profile`, `GET /profile/photos`, `POST /profile/photos/upload-url`, `GET /profile/preferences` |
| Discovery | `GET /discovery/candidates?cursor=`, `POST /discovery/swipe`, `GET /discovery/matches` |
| Communication | `GET /conversations`, `GET /conversations/{id}/messages?cursor=` |
| Moderation | `POST /moderation/report`, `POST /moderation/block/{userId}` |
| Admin | `GET /admin/users?q=`, `GET /admin/reports?status=`, `POST /admin/users/{id}/ban` |
| Media | `POST /media/upload-url`, `POST /media/confirm` |
| Notification | `GET /notifications?cursor=`, `POST /notifications/read` |

**SignalR Hubs**:

| Hub | Route | Methods |
|-----|-------|---------|
| `ChatHub` | `/hubs/chat` | `SendMessage`, `TypingIndicator`, `MarkRead` (server→client: `ReceiveMessage`, `MessageRead`, `TypingUpdate`) |
| `NotificationHub` | `/hubs/notifications` | Server→client: `NewNotification`, `BadgeUpdate` |

**Database schema convention**: `{context}.{table}` — e.g., `identity.users`, `profile.profiles`, `discovery.swipes`, `communication.messages`. Each EF Core `DbContext` maps to one schema via `OnModelCreating` → `modelBuilder.HasDefaultSchema("identity")`. PostGIS `geography(Point,4326)` on `profile.profiles.location` with GiST index.

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit (xUnit) | Domain entities, value objects, validators, MediatR handlers | Arrange-Act-Assert; mock repository interfaces |
| Integration (xUnit) | EF Core queries, SignalR connection lifecycle | Testcontainers (PostgreSQL+PostGIS); in-memory SignalR test host |
| E2E (Jasmine/Karma) | Angular component rendering, SignalR stub, form validation | ComponentTestBed; SignalR mock service; `HttpClientTestingModule` |
| Contract | OpenAPI spec generation | Swashbuckle validates responses match contracts |

No tests exist yet — scaffolding happens during `sdd-tasks` Phase 0.

## Migration / Rollout

None — greenfield project. Feature flags (config-based) for phased rollout: `EnableDiscovery`, `EnableChat`, `EnablePushNotifications`. DB migrations applied via `dotnet ef database update` in Docker compose startup.

## Open Questions

- [ ] SignalR + JWT refresh mid-connection: does reconnect carry the new token? Prototype needed.
- [ ] Angular Signals for infinite-scroll message history: adequate perf or need virtual scrolling?
- [ ] Azure Blob Storage emulator (Azurite) vs real Azure for dev; confirm team preference.
- [ ] Photo moderation queue: manual-only for MVP or integrate Azure Content Moderator from day one?
