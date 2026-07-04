# Proposal: Module 5 — JWT Identity

## Intent

Add authentication and identity management to the Dinder API. Users can register, login, refresh their token, and access a protected profile endpoint. Password hashing with BCrypt, JWT tokens with refresh rotation. Lightweight — no ASP.NET Core Identity.

## Scope

### In Scope
- `POST /auth/register` — email + password → hashed password → saved User → JWT
- `POST /auth/login` — email + password → verify hash → JWT + refresh token
- `POST /auth/refresh` — refresh token → new JWT (rotate refresh token)
- `GET /me` — `[Authorize]` protected endpoint, returns authenticated user info
- BCrypt password hashing (BCrypt.Net-Next)
- JWT generation and validation (Microsoft.AspNetCore.Authentication.JwtBearer)
- Request validation with FluentValidation
- Refresh token stored in User entity
- `appsettings.json` with JWT secret and expiration config

### Out of Scope
- ASP.NET Core Identity (too heavy for learning)
- Email verification / password reset
- Role-based authorization (roles only, no complex policies)
- Social login (Google, etc.)
- Refresh token revocation list

## Approach

Manual JWT with BCrypt. Register and Login use MediatR commands (not queries — they mutate state). JWT middleware configured in Program.cs. Protected endpoints use `[Authorize]` attribute.

## Estimated Impact
- ~300-350 lines changed
- New NuGet packages: 3
- New files: ~8
- Modified files: ~4
