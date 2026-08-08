# Tasks: Module 8 — User Profiles

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~300 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR (4 learning sessions) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain + Migration (Session 1) | — | Foundation: Gender enum, User enrichment, EF migration |
| 2 | Command + Validator (Session 2) | — | Depends on Unit 1 (User entity has new fields) |
| 3 | API + Response (Session 3) | — | Depends on Unit 2 (command exists) |
| 4 | Testing (Session 4) | — | Depends on Unit 3 (endpoint functional) |

## Session 1: Domain Enrichment & Migrations

- [x] 1.1 Create `src/Dinder.Domain/Enums/Gender.cs` — enum Male, Female, NonBinary, Other. **Concepts**: C# enums, Domain layer ownership. **Verify**: `dotnet build src/Dinder.Domain`
- [x] 1.2 Modify `src/Dinder.Domain/Entities/User.cs` — add 4 nullable properties: `string? DisplayName`, `string? Bio`, `DateOnly? BirthDate`, `Gender? Gender`. **Concepts**: entity enrichment, nullable vs defaults, DateOnly. **Verify**: `dotnet build src/Dinder.Domain`
- [x] 1.3 Modify `src/Dinder.Infrastructure/Persistence/DinderDbContext.cs` — Fluent API: DisplayName max 100, Bio max 500, Gender `HasConversion<string>()`, BirthDate column type `date`. **Concepts**: EF Fluent API, enum-as-string conversion, column types. **Verify**: `dotnet build src/Dinder.Infrastructure`
- [x] 1.4 Run `dotnet ef migrations add AddUserProfileFields` in `src/Dinder.Api` — inspect generated migration for 4 nullable columns. **Concepts**: EF add-migration, migration file anatomy. **Verify**: `dotnet ef migrations list`

## Session 2: Update Command Pattern

- [x] 2.1 Create `src/Dinder.Application/Common/Commands/Profiles/UpdateProfile/UpdateProfileCommand.cs` — record with DisplayName (string), Bio (string?), BirthDate (DateOnly?), Gender (Gender?), implementing `IRequest<MeResponse>`. **Concepts**: positional records, nullable command props, IRequest<T>. **Verify**: `dotnet build src/Dinder.Application`
- [x] 2.2 Create `src/Dinder.Application/Common/Commands/Profiles/UpdateProfile/UpdateProfileCommandValidator.cs` — FluentValidation: DisplayName not-empty + max 100, Bio max 500, BirthDate `LessThan(today)` + `Must(age >= 18)`, Gender `IsInEnum()`. **Concepts**: Must(), IsInEnum(), LessThan, custom messages. **Verify**: `dotnet build src/Dinder.Application`
- [x] 2.3 Create `src/Dinder.Application/Common/Commands/Profiles/UpdateProfile/UpdateProfileCommandHandler.cs` — extract email from JWT (IHttpContextAccessor), load User, mutate fields in-place, SaveChanges, return MeResponse. **Concepts**: update pattern (load-mutate-save), tracked entity mutation. **Verify**: `dotnet build src/Dinder.Application`

## Session 3: API Wiring & Response Expansion

- [x] 3.1 Modify `src/Dinder.Application/Common/Models/MeResponse.cs` — expand from 3 to 7 positional params: add string? DisplayName, string? Bio, DateOnly? BirthDate, Gender? Gender. **Concepts**: record evolution, positional record syntax. **Verify**: `dotnet build src/Dinder.Application`
- [x] 3.2 Modify `src/Dinder.Application/Common/Queries/Me/MeQueryHandler.cs` — map 4 new User fields into MeResponse (was 3 params, now 7). **Concepts**: handler response enrichment, null propagation. **Verify**: `dotnet build src/Dinder.Application`
- [x] 3.3 Modify `src/Dinder.Api/Program.cs` — add using for UpdateProfile namespace, register `app.MapPut("/me/profile", [Authorize] async (UpdateProfileCommand cmd, IMediator m) => m.Send(cmd))`. **Concepts**: PUT minimal API, route param binding with records, [Authorize] attribute. **Verify**: `dotnet build src/Dinder.Api`

## Session 4: Testing

- [x] 4.1 Create `tests/Dinder.UnitTests/Profiles/UpdateProfileCommandHandlerTests.cs` — 3 [Fact] tests: happy path (profile persists), user-not-found throws, email-claim-missing throws. Use InMemory EF + mock IHttpContextAccessor. **Concepts**: mocking HttpContext, InMemory provider, update handler testing. **Verify**: `dotnet test tests/Dinder.UnitTests --filter UpdateProfileCommandHandler`
- [x] 4.2 Modify `tests/Dinder.IntegrationTests/MeEndpointTests.cs` — change assertions from 3 fields to 7 (add displayName, bio, birthDate, gender). **Concepts**: test maintenance on response schema change. **Verify**: `dotnet test tests/Dinder.IntegrationTests --filter MeEndpoint`
- [x] 4.3 Create `tests/Dinder.IntegrationTests/ProfileEndpointTests.cs` — 3 tests: PUT→GET round-trip (200), PUT without JWT (401), PUT with DisplayName > 100 chars (400). Use CustomWebApplicationFactory + TestContainers PostgreSQL. **Concepts**: integration testing update endpoints, validation error assertions. **Verify**: `dotnet test tests/Dinder.IntegrationTests --filter ProfileEndpoint`

## Final Verification

- [x] `dotnet build` — entire solution compiles.
- [x] `dotnet test` — all new + existing tests green.
- [ ] `dotnet ef database update` (dev) — migration applies cleanly.
- [ ] Manual Swagger: register → login → PUT /me/profile → GET /me returns 7 fields with persisted values.
