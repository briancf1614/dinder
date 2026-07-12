# Exploration: Module 6 — API Gateway

## Current State

The project has ONE service (`Dinder.Api` on port 5022) with PostgreSQL. No reverse proxy, no multi-service routing. Docker only runs PostgreSQL. Projects are built individually — no `.sln` file.

**What we have:**
- 5 C# projects: Domain, Application, Infrastructure, Contracts, Api
- 2 test projects: UnitTests, IntegrationTests
- Docker: PostgreSQL 17-alpine on port 5432
- API: health, auth (register/login/refresh), /me with JWT

**What's missing for gateway:**
- No reverse proxy / API Gateway
- No second service to route to
- No internal Docker network for service-to-service communication
- No .sln file to manage multiple runnable projects

## Affected Areas

| Area | Impact |
|------|--------|
| `src/Dinder.Gateway/` | NEW — YARP reverse proxy project |
| `src/Dinder.Health/` | NEW — minimal health status service |
| `docker-compose.yml` | MODIFY — add gateway, health, internal network |
| Root directory | NEW — `.sln` file (optional, but recommended) |
| `LEARNING-PATH.md` | MODIFY — update module tracker |
| Tests | NEW — gateway integration tests (optional for this phase) |

## Approaches

### Approach 1 ✅ CHOSEN: nginx + Minimal Health API (separate .csproj)

```
Browser → http://localhost:8080 (Gateway - YARP)
              ├── /api/* → Dinder.Api (port 5022, internal)
              └── /health → Dinder.Health (port 5001, internal)
```

- **Gateway**: New `src/Dinder.Gateway/` ASP.NET Core Empty project with `Yarp.ReverseProxy` package. Configuration via `appsettings.json` (routes + clusters).
- **Health Service**: New `src/Dinder.Health/` ASP.NET Core Empty project. Single `/status` endpoint returning `{ "service": "health", "status": "ok" }`. Zero dependencies beyond ASP.NET Core.
- **Docker**: docker-compose with internal bridge network. Gateway exposed on 8080, API and Health on internal ports only.

Pros:
- Each service is its own .csproj — independent build, test, deploy
- Teaches real multi-service architecture
- YARP config is pure JSON, easy to understand
- Health service is so minimal (~30 lines) it doesn't distract from the gateway concept

Cons:
- Need to manage 2 new .csproj files
- Build chain more complex (gateway → routes to API → API needs PostgreSQL)
- Without .sln, running all services requires multiple terminal windows

Effort: **Medium** (~200 lines new code, mostly JSON config)

### Approach 2: YARP in the same project as Dinder.Api

```
Browser → Dinder.Api (port 5022)
              ├── /api/* → itself (or pass through)
              └── /health → external health service
```

Add YARP as middleware inside the existing `Dinder.Api` project, routing to an external health service.

Pros:
- Fewer projects to manage
- Faster to implement (no new .csproj)

Cons:
- **Defeats the purpose** — gateway should be separate from the services it routes to
- Teaches wrong architecture (coupling gateway to API)
- When you scale, you'd need to extract it anyway
- Less learning value

Effort: **Low** (but wrong pattern)

### Approach 3: nginx instead of YARP

Use nginx as reverse proxy in Docker, keep the backend in .NET.

Pros:
- nginx is the industry standard for reverse proxy
- Lightweight Docker image (~5MB)
- Rate limiting built-in
- Teaches real-world DevOps

Cons:
- Not .NET — different tech stack to learn
- Config is nginx-specific syntax, not JSON
- Goal is learning .NET ecosystem, not general DevOps
- Harder to integrate with .NET tooling (logging, DI)

Effort: **Medium** (but different tech stack)

## Recommendation

**Approach 1: YARP + Minimal Health API.**

Rationale:
1. **YARP is the .NET-native gateway** — same ecosystem, same patterns, same logging. You learn ONE stack.
2. **Separate projects = real microservices** — even though tiny, the architecture is correct from day 1.
3. **Gateway truly routes** — not a passthrough, but a real reverse proxy with two backend destinations.
4. **Health service is intentionally minimal** — you focus on the GATEWAY concept, not on building another CRUD.
5. **Scales naturally** — when we add Profiles, Chat, etc., just add more routes to YARP config.

## Docker Networking Strategy

```
docker-compose.yml:
  services:
    gateway:        # YARP - exposed on port 8080
      depends_on: [api, health]
    api:            # Dinder - internal only (no ports exposed)
      depends_on: [postgres]
    health:         # Health Service - internal only
    postgres:       # existing
  networks:
    dinder-net:     # internal bridge — all services communicate here
```

Gateway is the ONLY service exposed to the host. API and Health talk only through the internal network. This is how production works.

## Risks

- **No .sln file**: Running 3 services (gateway + API + health + postgres) without orchestration is messy. Recommendation: create a minimal `.sln` or rely entirely on docker-compose.
- **Build order**: Gateway references nothing from other projects (it's a reverse proxy), so no build dependency. Health is standalone. API already builds independently. No circular dependency risk.
- **Port conflicts**: Need to ensure API stays on 5022 internally but only accessible through gateway. docker-compose handles this.
- **Testing**: Gateway is hard to unit test (it's config, not code). Focus on integration test: spin up docker-compose, hit gateway, verify routing.

## Ready for Proposal

**Yes.** Clear direction: YARP gateway + Health Service as separate projects, docker-compose orchestration, no .sln needed (docker-compose is the orchestrator).
