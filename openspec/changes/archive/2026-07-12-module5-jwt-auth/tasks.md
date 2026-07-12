# Tasks: Module 5 — JWT Identity

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~370 |
| 400-line budget risk | Medium |
| Chained PRs recommended | Yes |
| Suggested split | Session 1 → Session 2 → Session 3 |
| Delivery strategy | ask-always |
| Chain strategy | stacked-to-main |

## Session 1: Foundation + Register

- [ ] 1.1 Modify `src/Dinder.Domain/Entities/User.cs` — add `RefreshToken` (string?), `RefreshTokenExpiry` (DateTime?), `Role` (string, default "user"). **Concepts**: entity evolution, nullable vs default. **Verify**: `dotnet build src/Dinder.Domain`
- [ ] 1.2 Modify `src/Dinder.Api/appsettings.json` — add `Jwt` section (Secret, Issuer, Audience, ExpirationMinutes). **Concepts**: configuration patterns. **Verify**: build
- [ ] 1.3 Modify `src/Dinder.Api/Dinder.Api.csproj` — add `Microsoft.AspNetCore.Authentication.JwtBearer` + `BCrypt.Net-Next`. Modify `src/Dinder.Application/Dinder.Application.csproj` — add `FluentValidation` + `FluentValidation.DependencyInjectionExtensions`. **Concepts**: NuGet management. **Verify**: `dotnet restore`, `dotnet build`
- [ ] 1.4 Create `src/Dinder.Application/Common/Interfaces/ITokenService.cs` — interface with `string GenerateToken(User user)` and `string GenerateRefreshToken()`. **Concepts**: interface segregation. **Verify**: build
- [ ] 1.5 Create `src/Dinder.Infrastructure/Services/TokenService.cs` — implements ITokenService. Reads Jwt config from IConfiguration. Uses `JwtSecurityTokenHandler` to create token with email claim, issuer, audience, expiry. GenerateRefreshToken returns `Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`. **Concepts**: JWT internals, claims, signing credentials. **Verify**: build
- [ ] 1.6 Create `src/Dinder.Application/Common/Models/AuthResponse.cs` — record with Token (string) and RefreshToken (string). **Concepts**: record types vs classes. **Verify**: build
- [ ] 1.7 Create `src/Dinder.Application/Common/Commands/Auth/Register/RegisterCommand.cs` — record with Email and Password. IRequest<AuthResponse>. **Concepts**: command pattern. **Verify**: build
- [ ] 1.8 Create `src/Dinder.Application/Common/Commands/Auth/Register/RegisterCommandValidator.cs` — FluentValidation: Email not empty + email format, Password minimum 8 chars. **Concepts**: FluentValidation rules. **Verify**: build
- [ ] 1.9 Create `src/Dinder.Application/Common/Commands/Auth/Register/RegisterCommandHandler.cs` — check email uniqueness, BCrypt hash, save User, call TokenService, return AuthResponse. **Concepts**: BCrypt hashing, conflict detection. **Verify**: build
- [ ] 1.10 Modify `src/Dinder.Api/Program.cs` — add JWT auth (AddAuthentication + AddJwtBearer), register TokenService (AddScoped), register FluentValidation, add `POST /auth/register` endpoint calling MediatR. **Concepts**: auth middleware pipeline. **Verify**: `dotnet build`
- [ ] 1.11 Add EF migration — run `dotnet ef migrations add AddRefreshTokenAndRole`. **Concepts**: schema evolution. **Verify**: migration file created

## Session 2: Login + Refresh + Me

- [ ] 2.1 Create `src/Dinder.Application/Common/Commands/Auth/Login/LoginCommand.cs` — record with Email, Password. IRequest<AuthResponse>. **Verify**: build
- [ ] 2.2 Create `src/Dinder.Application/Common/Commands/Auth/Login/LoginCommandValidator.cs` — same rules as Register. **Verify**: build
- [ ] 2.3 Create `src/Dinder.Application/Common/Commands/Auth/Login/LoginCommandHandler.cs` — find user by email, BCrypt.Verify, generate tokens, save refresh token to User, return AuthResponse. Throw UnauthorizedAccessException on mismatch. **Verify**: build
- [ ] 2.4 Create `src/Dinder.Application/Common/Commands/Auth/Refresh/RefreshCommand.cs` — record with RefreshToken. IRequest<AuthResponse>. **Verify**: build
- [ ] 2.5 Create `src/Dinder.Application/Common/Commands/Auth/Refresh/RefreshCommandHandler.cs` — find user by refresh token + not expired, rotate refresh token, generate new JWT, save. **Verify**: build
- [ ] 2.6 Create `src/Dinder.Application/Common/Queries/Me/MeQuery.cs` — record, IRequest<MeResponse>. **Verify**: build
- [ ] 2.7 Create `src/Dinder.Application/Common/Queries/Me/MeQueryHandler.cs` — extract email from IHttpContextAccessor User claims, find user, return MeResponse (Id, Email, CreatedAt). **Verify**: build
- [ ] 2.8 Create `src/Dinder.Application/Common/Models/MeResponse.cs` — record with Id, Email, CreatedAt. **Verify**: build
- [ ] 2.9 Modify `src/Dinder.Api/Program.cs` — add `POST /auth/login`, `POST /auth/refresh`, `GET /me` with `[Authorize]`. Register IHttpContextAccessor. **Concepts**: [Authorize], endpoint protection. **Verify**: build

## Session 3: Tests

- [ ] 3.1 Create `tests/Dinder.UnitTests/Auth/RegisterCommandHandlerTests.cs` — 3 tests: successful registration, duplicate email throws, password is hashed. **Verify**: `dotnet test --filter Register`
- [ ] 3.2 Create `tests/Dinder.UnitTests/Auth/LoginCommandHandlerTests.cs` — 2 tests: valid login returns tokens, wrong password throws. **Verify**: `dotnet test --filter Login`
- [ ] 3.3 Create `tests/Dinder.IntegrationTests/AuthEndpointTests.cs` — 2 tests: POST /auth/register → 200 + token, POST /auth/login → 200 + token. **Verify**: `dotnet test --filter AuthEndpoint`
- [ ] 3.4 Create `tests/Dinder.IntegrationTests/MeEndpointTests.cs` — 2 tests: GET /me with token → 200, GET /me without token → 401. **Verify**: `dotnet test --filter MeEndpoint`

## Final Verification
- [ ] Run full suite: `dotnet test` — all tests green.
- [ ] Manual test: register → login → GET /me with token → refresh
