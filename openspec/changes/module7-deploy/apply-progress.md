# Apply Progress: Deploy Dinder to Production (Oracle ARM)

**Date**: 2026-07-12
**Batch**: 1/1 (single batch — all 17 tasks)
**Mode**: Strict TDD (openspec artifact store)
**Test runner**: dotnet test (xUnit 2.9.3)

## Safety Net

| Check | Result |
|-------|--------|
| Unit tests (pre) | ✅ 14/14 passing |
| Integration tests (pre) | ⚠️ 8/10 failing — pre-existing Docker daemon unavailable on Windows (Testcontainers requires Docker) |
| Unit tests (post, after Phase 1-2) | ✅ 14/14 passing |
| Unit tests (final) | ✅ 14/14 passing |

No .NET code was modified — all changes are YAML/nginx config files. The integration test failures are pre-existing and unrelated to these changes.

## TDD Cycle Evidence

| Task | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-------|------------|-----|-------|-------------|----------|
| 1.1 | Config | ✅ 14/14 | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 1.2 | Config | ✅ 14/14 | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 1.3 | Config | ✅ 14/14 | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 1.4 | Config | ✅ 14/14 | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 1.5 | Config | ✅ 14/14 | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 1.6 | Config | ✅ 14/14 | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 2.1 | Config | ✅ 14/14 | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 2.2 | Config | ✅ 14/14 | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 3.1 | Config | N/A (new) | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 3.2 | Config | N/A (new) | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 3.3 | Config | N/A (new) | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |
| 3.4 | Config | N/A (new) | ➖ Structural | ✅ Validated | ➖ Skipped | ➖ None needed |

**Triangulation skipped for all tasks**: All 12 implemented tasks are purely structural configuration changes (YAML/nginx conf). Each task produces exactly ONE possible output with zero branching or logic. Tasks directly implement design.md specs with no computation to test.

### Test Summary
- **Total tests written**: 0 (purely structural config changes — no .NET code modified)
- **Total tests passing**: 14 (pre-existing unit tests, all still passing)
- **Layers used**: Config (structural — no code test layer applicable)
- **Approval tests**: None — no refactoring tasks
- **Pure functions created**: 0 (no application code modified)

## Completed Tasks

### Phase 1: Production Compose Hardening (6/6)
- [x] 1.1 Removed `ports: 5432:5432` from postgres
- [x] 1.2 Added `restart: unless-stopped` to all 4 services
- [x] 1.3 Added `ASPNETCORE_ENVIRONMENT=Production` to api
- [x] 1.4 Added healthcheck to api (curl localhost:5022/health, interval 30s)
- [x] 1.5 Added healthcheck to health (curl localhost:5001/health, interval 30s)
- [x] 1.6 Changed nginx port from `8080:80` to `80:80`

### Phase 2: Gateway Production Update (2/2)
- [x] 2.1 Added `access_log /var/log/nginx/access.log;`
- [x] 2.2 Added `error_log /var/log/nginx/error.log warn;`

### Phase 3: CI/CD Deploy Pipeline (4/4)
- [x] 3.1 Created `.github/workflows/deploy.yml` (deploy job on push to main)
- [x] 3.2 Added pre-deploy `dotnet test` step
- [x] 3.3 Added SSH deploy step via `appleboy/ssh-action@v1`
- [x] 3.4 Added post-deploy smoke test (curl health endpoints)

### Phase 4: Manual Steps & Verification (0/5 — documented, not executable in apply)
- [ ] 4.1-4.2: Full step-by-step instructions written in tasks.md
- [ ] 4.3-4.5: Verification procedures documented in tasks.md

## Files Changed

| File | Action | Change Summary |
|------|--------|---------------|
| `docker-compose.yml` | Modified | Removed postgres host port, added restart policies, healthchecks, ASPNETCORE_ENVIRONMENT=Production, nginx port 80:80 |
| `nginx.conf` | Modified | Added access_log and error_log directives for production observability |
| `.github/workflows/deploy.yml` | **Created** | Full GitHub Actions SSH deploy pipeline with pre-deploy tests and post-deploy smoke |
| `openspec/changes/module7-deploy/tasks.md` | Modified | Marked 12 tasks [x], documented Phase 4 manual steps |

## Deviations from Design

None — implementation matches design.md exactly.

Minor additions beyond the design's minimum spec:
- Added `timeout: 10s`, `retries: 3`, `start_period: 40s` to healthcheck stanzas (Docker Compose best practice — design only specified interval)
- Separated smoke test into its own step with `sleep 5` before curl (design said "final step" — separation improves failure isolation)

## Issues Found

1. **Pre-existing integration test failures**: 8/10 integration tests fail due to Docker daemon not being available on this Windows machine (Testcontainers.PostgreSql requires Docker). These failures are unrelated to this change and predate it. Unit tests (14/14) are the relevant safety net and all pass.

## Remaining Tasks (Phase 4 — Manual)

- [ ] 4.1 Create Oracle Cloud Security List ingress rule for port 80
- [ ] 4.2 Configure GitHub repo secrets
- [ ] 4.3 Push to main, verify workflow deploys successfully
- [ ] 4.4 Verify full auth flow through gateway on port 80
- [ ] 4.5 Verify PostgreSQL port 5432 is NOT exposed on host

## Workload / PR Boundary

- Mode: Single PR
- Current work unit: Single PR (all 12 automatable tasks in one batch)
- Boundary: docker-compose.yml hardening → nginx conf → GitHub Actions deploy workflow → Phase 4 documentation
- Estimated review budget impact: ~64 lines changed (18 modified + 46 new) — well under 400-line budget

## Status

12/17 tasks complete (Phases 1-3). Phase 4 tasks (4.1-4.5) are manual server-side steps documented in tasks.md. Ready for manual deploy and verify.
