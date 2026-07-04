# Design: Module 5 — JWT Identity

## Technical Approach

Manual JWT + BCrypt. MediatR commands for mutations (Register, Login, Refresh), query for reading (Me). TokenService in Infrastructure for JWT generation. FluentValidation in Application for request validation.

**Stack additions**: BCrypt.Net-Next 4.x, Microsoft.AspNetCore.Authentication.JwtBearer 10.x, FluentValidation 11.x, FluentValidation.DependencyInjectionExtensions 11.x.

## Architecture Decisions

### AD-1: BCrypt vs ASP.NET Core Identity

| Option | Tradeoff |
|--------|----------|
| **BCrypt (chosen)** | 1 NuGet package, 2 lines of code. Full control. Learn how hashing works. |
| ASP.NET Core Identity | UserManager, SignInManager, RoleManager — 10+ classes to understand. Too much for a learning module. |

**Rationale**: Identity is a black box when learning. BCrypt makes the hashing visible. We can always migrate to Identity later if needed.

### AD-2: JWT Lifetime

| Token | Lifetime | Rationale |
|-------|----------|-----------|
| Access Token (JWT) | 15 minutes | Short-lived. If stolen, damage is limited. |
| Refresh Token | 7 days | Longer convenience. Rotated on each use. |

### AD-3: Refresh Token Storage

**Choice**: Store `RefreshToken` (string) and `RefreshTokenExpiry` (DateTime?) in the User entity.
**Rationale**: Simple, no extra table. One user = one refresh token. Production apps might use a separate table for multi-device, but that's overkill here.

### AD-4: Command vs Query for Auth

**Choice**: Register, Login, Refresh are Commands (mutate state). Me is a Query (read-only).
**Rationale**: Follows CQRS. Register creates a User, Login updates RefreshToken, Refresh rotates tokens. Me just reads.

## File Changes

| # | File | Action | Description |
|---|------|--------|-------------|
| 1 | `src/Dinder.Api/Dinder.Api.csproj` | Modify | Add JwtBearer, BCrypt.Net-Next |
| 2 | `src/Dinder.Application/Dinder.Application.csproj` | Modify | Add FluentValidation |
| 3 | `src/Dinder.Api/Program.cs` | Modify | Add JWT middleware, register services, new endpoints |
| 4 | `src/Dinder.Api/appsettings.json` | Modify | Add Jwt section (secret, issuer, audience) |
| 5 | `src/Dinder.Domain/Entities/User.cs` | Modify | Add RefreshToken, RefreshTokenExpiry, Role |
| 6 | `src/Dinder.Application/Common/Interfaces/ITokenService.cs` | Create | Interface for JWT generation |
| 7 | `src/Dinder.Infrastructure/Services/TokenService.cs` | Create | JWT generation with config |
| 8 | `src/Dinder.Application/Common/Commands/Auth/Register/RegisterCommand.cs` | Create | Register DTO |
| 9 | `src/Dinder.Application/Common/Commands/Auth/Register/RegisterCommandHandler.cs` | Create | Hash + save + JWT |
| 10 | `src/Dinder.Application/Common/Commands/Auth/Register/RegisterCommandValidator.cs` | Create | FluentValidation rules |
| 11 | `src/Dinder.Application/Common/Commands/Auth/Login/LoginCommand.cs` | Create | Login DTO |
| 12 | `src/Dinder.Application/Common/Commands/Auth/Login/LoginCommandHandler.cs` | Create | Verify hash + generate tokens |
| 13 | `src/Dinder.Application/Common/Commands/Auth/Login/LoginCommandValidator.cs` | Create | FluentValidation rules |
| 14 | `src/Dinder.Application/Common/Commands/Auth/Refresh/RefreshCommand.cs` | Create | Refresh DTO |
| 15 | `src/Dinder.Application/Common/Commands/Auth/Refresh/RefreshCommandHandler.cs` | Create | Validate + rotate |
| 16 | `src/Dinder.Application/Common/Models/AuthResponse.cs` | Create | Response DTO (Token, RefreshToken) |
| 17 | `src/Dinder.Application/Common/Queries/Me/MeQuery.cs` | Create | Me query |
| 18 | `src/Dinder.Application/Common/Queries/Me/MeQueryHandler.cs` | Create | Read user from JWT |
| 19 | Migration file | Create | Add RefreshToken, RefreshTokenExpiry, Role to User |

## Data Flow

```
Register:
  POST /auth/register { email, password }
    → RegisterCommandValidator (FluentValidation)
    → RegisterCommandHandler
        → BCrypt.HashPassword(password)
        → new User { Email, PasswordHash = hash }
        → dbContext.Users.Add(user)
        → dbContext.SaveChanges()
        → tokenService.GenerateToken(user)
    → 200 { token, refreshToken }

Login:
  POST /auth/login { email, password }
    → LoginCommandValidator
    → LoginCommandHandler
        → dbContext.Users.FirstOrDefault(email)
        → BCrypt.Verify(password, user.PasswordHash)
        → tokenService.GenerateToken(user)
        → user.RefreshToken = random
        → dbContext.SaveChanges()
    → 200 { token, refreshToken }

Refresh:
  POST /auth/refresh { refreshToken }
    → RefreshCommandHandler
        → dbContext.Users.FirstOrDefault(r => r.RefreshToken == token)
        → user.RefreshToken = newRandom (rotation)
        → tokenService.GenerateToken(user)
    → 200 { token, refreshToken }

GET /me [Authorize]:
    → MeQueryHandler
        → email from JWT claims
        → dbContext.Users.FirstOrDefault(email)
    → 200 { id, email, createdAt }
```

## JWT Configuration (appsettings.json)
```json
{
  "Jwt": {
    "Secret": "super-secret-key-min-32-chars-long-for-hs256!!",
    "Issuer": "dinder-api",
    "Audience": "dinder-app",
    "ExpirationMinutes": 15
  }
}
```
