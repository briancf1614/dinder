# Proposal: Module 6 — API Gateway (nginx)

## Intent

Add a real reverse proxy with nginx to the Dinder architecture. Route requests to two backend services. Learn how API Gateways work in production — the concept behind nginx, Traefik, Envoy, Kong, and every other gateway.

## Scope

### In Scope
- **nginx** Docker container as reverse proxy (entry point on port 8080)
- Route `/api/*` → `Dinder.Api` (internal, port 5022)
- Route `/health` → new `Dinder.Health` service (internal, port 5001)
- nginx config with two `location` blocks — teach the reverse proxy concept
- **`src/Dinder.Health/`** — new minimal .NET API project (30 lines)
  - `GET /health` → `{ "service": "health", "status": "ok" }`
  - Zero dependencies beyond ASP.NET Core Minimal APIs
- **docker-compose** updated: nginx + health service + internal bridge network
- Gateway is the ONLY service exposed to the host — API and Health are internal

### Out of Scope
- Load balancing (multiple API instances — just one for now)
- SSL/TLS termination (we'll add later with real certs)
- Rate limiting (nginx supports it, but save for a focused module)
- Caching at gateway level
- YARP (decided: nginx teaches transferable skills)

## Approach

nginx reverse proxy in Docker. Config lives in `nginx.conf`. All services in docker-compose with internal network. This is the standard pattern used by startups and enterprises alike.

## Learning Objectives

1. **Reverse proxy concept**: Client → Gateway → Backend. Why hide services behind a gateway.
2. **Path-based routing**: `/api/*` vs `/health` — different backends based on URL.
3. **Docker networking**: Internal network, service discovery via container names.
4. **nginx config syntax**: `server`, `location`, `proxy_pass`.
5. **Architecture evolution**: From monolith (one service exposed) to gateway pattern (one entry point, many backends).

## Estimated Impact
- New files: `nginx.conf`, `src/Dinder.Health/` project (~5 files)
- Modified files: `docker-compose.yml`
- New lines: ~100 total (nginx config + Health API + compose)
- No changes to existing `Dinder.Api` code
