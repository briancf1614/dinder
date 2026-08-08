# Design: Module 8 — User Profiles

## Technical Approach

Enrich the User entity with 4 nullable profile columns via EF Core migration. Add a `PUT /me/profile` [Authorize] endpoint using the established CQRS pattern: command record → validator → handler. Handler extracts user from JWT (same pattern as MeQueryHandler), updates fields in-place on the tracked entity, saves. MeResponse expands from 3 to 7 fields. No new NuGet packages required.

## Architecture Decisions

### AD-1: PUT full-replacement vs PATCH

| Option | Tradeoff |
|--------|----------|
| **PUT (chosen)** | Full object in request body. Simpler handler, one clear contract. All 4 fields sent every time. |
| PATCH (JSON Merge) | Partial updates. Needs JsonPatchDocument, more complex. Not worth it for 4 fields. |

**Rationale**: PUT is the learning-appropriate choice. Profile is a single resource — sending all 4 fields is cheap. PATCH adds plumbing without teaching a new concept.

### AD-2: Gender enum location

| Option | Tradeoff |
|--------|----------|
| **Domain/Enums/Gender.cs (chosen)** | Clean, no dependency. User.cs references it directly. |
| Application layer enum | Wrong layer — Gender is a domain concept, not an application concern. |

**Rationale**: Gender belongs in Domain. It's a value the entity holds. No reason to put it elsewhere.

### AD-3: Profile fields — nullable vs default values

| Option | Tradeoff |
|--------|----------|
| **Nullable (chosen)** | Existing users get NULL. No migration data issues. Explicit "not set" state. |
| Default values | Empty strings, MinValue dates. Ambiguous — is "" really "not set"? |

**Rationale**: NULL is the honest representation of "user hasn't set this yet." Existing users in dev DB keep NULL.

## Data Flow

```
PUT /me/profile { displayName, bio, birthDate, gender }
  → UpdateProfileCommandValidator (FluentValidation pipeline)
  → UpdateProfileCommandHandler
      → email from JWT ClaimTypes.Email (IHttpContextAccessor)
      → dbContext.Users.FirstOrDefaultAsync(email)
      → user.DisplayName = command.DisplayName
      → user.Bio = command.Bio
      → user.BirthDate = command.BirthDate
      → user.Gender = command.Gender
      → dbContext.SaveChangesAsync()
  → 200 MeResponse(id, email, createdAt, displayName, bio, birthDate, gender)

GET /me [Authorize]  (modified)
  → MeQueryHandler
      → email from JWT
      → dbContext.Users.FirstOrDefaultAsync(email)
  → 200 MeResponse with 7 fields (was 3)
```

## File Changes

| # | File | Action | Description |
|---|------|--------|-------------|
| 1 | `src/Dinder.Domain/Enums/Gender.cs` | Create | Gender enum: Male, Female, NonBinary, Other |
| 2 | `src/Dinder.Domain/Entities/User.cs` | Modify | Add DisplayName, Bio, BirthDate (DateOnly?), Gender (Gender?) |
| 3 | `src/Dinder.Infrastructure/Persistence/DinderDbContext.cs` | Modify | Fluent API: column max lengths, enum as string |
| 4 | `src/Dinder.Application/Common/Commands/Profiles/UpdateProfile/UpdateProfileCommand.cs` | Create | Record: 4 profile fields, returns MeResponse |
| 5 | `src/Dinder.Application/Common/Commands/Profiles/UpdateProfile/UpdateProfileCommandHandler.cs` | Create | Extract user, update fields, save, return MeResponse |
| 6 | `src/Dinder.Application/Common/Commands/Profiles/UpdateProfile/UpdateProfileCommandValidator.cs` | Create | FluentValidation: required, length, age, enum |
| 7 | `src/Dinder.Application/Common/Models/MeResponse.cs` | Modify | 3→7 positional params: add DisplayName, Bio, BirthDate, Gender |
| 8 | `src/Dinder.Application/Common/Queries/Me/MeQueryHandler.cs` | Modify | Map 4 new fields from User to MeResponse |
| 9 | `src/Dinder.Api/Program.cs` | Modify | Register `app.MapPut("/me/profile", ...)` endpoint |
| 10 | Migration file | Create | `dotnet ef migrations add AddUserProfileFields` |
| 11 | `tests/Dinder.UnitTests/Profiles/UpdateProfileCommandHandlerTests.cs` | Create | Happy path + validation edge cases |
| 12 | `tests/Dinder.IntegrationTests/ProfileEndpointTests.cs` | Create | PUT → GET round-trip, 401 without token |
| 13 | `tests/Dinder.IntegrationTests/MeEndpointTests.cs` | Modify | Assert 7 fields in response (was 3) |

## Interfaces / Contracts

```csharp
// PUT /me/profile request body
public record UpdateProfileCommand(
    string DisplayName,
    string? Bio,
    DateOnly? BirthDate,
    Gender? Gender
) : IRequest<MeResponse>;

// GET /me and PUT /me/profile response (was 3 fields, now 7)
public record MeResponse(
    Guid Id,
    string Email,
    DateTime CreatedAt,
    string? DisplayName,
    string? Bio,
    DateOnly? BirthDate,
    Gender? Gender
);

// Domain enum
public enum Gender { Male, Female, NonBinary, Other }
```

## Migration Strategy

`dotnet ef migrations add AddUserProfileFields` generates 4 nullable columns on the Users table. No seed data, no backward-fill needed — existing rows stay NULL. Rollback: `dotnet ef migrations remove` + git revert.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | UpdateProfileCommandHandler happy path | EF Core InMemory, mock IHttpContextAccessor |
| Unit | Validation edge cases | Each invalid input → ValidationException |
| Integration | PUT /me/profile → GET /me round-trip | CustomWebApplicationFactory + real PostgreSQL container |
| Integration | 401 without JWT | No Authorization header → 401 |

## CONCEPTOS.md Topics

- **Entity enrichment**: adding columns to an existing EF entity without breaking data
- **Update command pattern**: first non-create CQRS mutation — load, mutate, save
- **C# enums with EF Core**: `HasConversion<string>()` so values are readable in DB
- **DateOnly**: .NET 6+ type, maps to PostgreSQL `date`, avoids timezone issues
- **FluentValidation Must()**: custom rule for 18+ age check combining LessThan + Must
- **PUT vs PATCH**: full replacement when the resource is a single unit

## Open Questions

- None — all decisions resolved from proposal and existing codebase patterns.
