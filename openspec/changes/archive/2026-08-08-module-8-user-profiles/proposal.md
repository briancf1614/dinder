# Proposal: Module 8 — User Profiles

## Intent

Users can set their dating profile after registration. Add 4 profile fields to the User entity (DisplayName, Bio, BirthDate, Gender) and a PUT endpoint to update them. Profile skeleton only — no photos or preferences yet.

## Learning Objectives

- Enrich an existing entity with EF Core migrations
- Write update commands (first non-create CQRS mutation)
- Design advanced FluentValidation rules (range, enum, conditional)
- Work with C# enums and DateOnly

## Scope

### In Scope
- 4 new fields on User: `DisplayName` (string, max 100, required), `Bio` (string, max 500, optional), `BirthDate` (DateOnly, past, 18+), `Gender` (enum: Male, Female, NonBinary, Other)
- EF Core migration to add columns to Users table
- `PUT /me/profile` [Authorize] — UpdateProfileCommand → returns expanded MeResponse
- FluentValidation: DisplayName required + length, Bio length, BirthDate range + age check, Gender enum membership
- Expanded MeResponse from 3 fields (Id, Email, CreatedAt) to 7 (add DisplayName, Bio, BirthDate, Gender)
- Gender enum in Domain layer

### Out of Scope
- Photos / image upload
- Discovery preferences (age range, distance, gender preference)
- AutoMapper (manual mapping only)
- GET other users' profiles
- Profile completeness tracking

## Capabilities

### New Capabilities
- `user-profiles`: profile data management — PUT endpoint for updating profile fields, expanded GET /me response with profile data, validation rules for all profile fields

### Modified Capabilities
- `identity`: User entity gains 4 nullable profile columns; `/me` response expands from 3 fields to 7; new `PUT /me/profile` endpoint registered in Program.cs

## Approach

Enrich User entity → add migration → write UpdateProfileCommand with validator → register PUT endpoint in Program.cs. Handler extracts user from JWT (same pattern as MeQuery), updates fields in-place on the tracked entity, saves. Same 3-file pattern per command: Command + Validator + Handler. Manual mapping in handler — no AutoMapper.

## Estimated Impact
- ~120-150 lines changed
- New NuGet packages: 0
- New files: ~5 (Command + Validator + Handler + Gender enum + migration)
- Modified files: ~3 (User.cs, MeResponse.cs, Program.cs)

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Migration breaks existing data | Low | New columns nullable or have safe defaults; dev DB has no real users |
| BirthDate validation edge cases (leap years, timezones) | Low | FluentValidation `LessThan` + `Must(age >= 18)` rule |

## Rollback Plan

`dotnet ef migrations remove`, delete new files under `Commands/Profiles/`, revert `User.cs` and `MeResponse.cs` via git.

## Dependencies

- Module 5 (JWT/Identity) — User entity, `/me` endpoint, `[Authorize]` middleware
- Module 3 (EF Core) — migrations configured and working

## Success Criteria

- [ ] `PUT /me/profile` accepts valid profile data and persists to DB
- [ ] `GET /me` returns full profile fields (DisplayName, Bio, BirthDate, Gender)
- [ ] Validation rejects: DisplayName > 100 chars, BirthDate < 18 years, invalid Gender, empty DisplayName
- [ ] Unit tests for UpdateProfileCommandHandler (happy path + validation)
- [ ] Integration test for PUT → GET round-trip
- [ ] CONCEPTOS.md updated with entity enrichment, update commands, enums, DateOnly
