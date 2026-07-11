# Tasks: Module 6 — API Gateway (nginx)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~130 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Delivery strategy | single-pr |
| Suggestion | One PR for everything — small change, no need to split |

---

## Session 1: Health Service

- [ ] 1.1 Create `src/Dinder.Health/Dinder.Health.csproj` — ASP.NET Core Web project, `net10.0`, no NuGet packages. **Concepts**: minimal project structure, what makes a project "web". **Verify**: `dotnet build src/Dinder.Health`
- [ ] 1.2 Create `src/Dinder.Health/Program.cs` — single `MapGet("/health", () => ...)` endpoint returning `{ service, status }`. **Concepts**: Minimal API, anonymous types, one-liner endpoints. **Verify**: `dotnet run --project src/Dinder.Health` → `curl localhost:5001/health`
- [ ] 1.3 Create `src/Dinder.Health/Properties/launchSettings.json` — port 5001, no HTTPS. **Concepts**: launch profiles, port binding. **Verify**: `dotnet run --project src/Dinder.Health --launch-profile http`

---

## Session 2: nginx Config

- [ ] 2.1 Create `nginx.conf` at repo root — `events {}`, `http { server { listen 80; } }` with two `location` blocks for `/api/` and `/health`. **Concepts**: nginx syntax, `proxy_pass`, location matching, upstream servers. **Verify**: dry-run with `docker run --rm -v ./nginx.conf:/etc/nginx/nginx.conf:ro nginx:alpine nginx -t`

---

## Session 3: Docker Compose Orchestration

- [ ] 3.1 Modify `docker-compose.yml` — add `nginx` service (alpine image, port 8080:80, config volume). **Concepts**: Docker volumes, port mapping. **Verify**: nginx container starts
- [ ] 3.2 Modify `docker-compose.yml` — add `health` service (build from `src/Dinder.Health/`). **Concepts**: Docker build context, service dependencies. **Verify**: health container starts
- [ ] 3.3 Modify `docker-compose.yml` — add `dinder-net` bridge network, connect all services. Remove `ports:` from `api` service (internal only). **Concepts**: Docker networking, internal vs exposed ports. **Verify**: `docker compose ps` shows all 4 services
- [ ] 3.4 Create `src/Dinder.Api/Dockerfile` — multi-stage build (SDK → build → publish → runtime). **Concepts**: Docker multi-stage builds, .NET Docker images. **Verify**: `docker build -t dinder-api src/Dinder.Api`
- [ ] 3.5 Create `src/Dinder.Health/Dockerfile` — same pattern, smaller (no DB dependencies). **Verify**: `docker build -t dinder-health src/Dinder.Health`

---

## Session 4: Integration & Verification

- [ ] 4.1 Full system test — `docker compose up`, then `curl http://localhost:8080/health` → 200 from Health Service. **Verify**: response matches spec
- [ ] 4.2 Gateway-to-API test — `curl http://localhost:8080/api/` → "Dinder API running!" from Dinder.Api. **Verify**: response passes through gateway
- [ ] 4.3 Auth through gateway — `curl -X POST http://localhost:8080/api/auth/register -H 'Content-Type: application/json' -d '{"email":"gw@test.com","password":"Test1234!"}'` → 200 + token. **Verify**: JWT returned
- [ ] 4.4 Internal-only test — verify that `http://localhost:5022` and `http://localhost:5001` are UNREACHABLE from host. **Verify**: connection refused

---

## Final Verification
- [ ] Run `docker compose up` — all 4 services healthy
- [ ] Register through gateway → login through gateway → GET /me through gateway
- [ ] Verify Health Service works independently (can be reached through gateway)
- [ ] Run existing test suite: `dotnet test` — 24/24 green (Dinder.Api unchanged)
