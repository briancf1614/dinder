# Proposal: Module 4 — Testing

## Intent

Add retroactive test coverage to existing Modules 1–3 code. Teach the user unit vs. integration testing through hands-on test writing. No new features — tests only.

## Scope

### In Scope
- Two test projects: `tests/Dinder.UnitTests` and `tests/Dinder.IntegrationTests`
- Unit tests for `HealthCheckQueryHandler`, `HealthCheckResult`, `User` entity
- EF Core configuration tests (model validation without DB)
- Integration tests with real PostgreSQL (DbContext CRUD, unique constraint, migrations)
- API endpoint tests via `WebApplicationFactory<Program>` (`GET /health`, `GET /`)
- Code coverage measurement via coverlet

### Out of Scope
- TDD workflow (Module 5)
- FluentAssertions (built-in xUnit asserts only)
- Per-layer test projects (overkill at this stage)
- New application features
- CI/CD pipeline for tests

## Capabilities

### New Capabilities
- `unit-testing`: xUnit-based test project for domain and application layer logic with Moq mocking
- `integration-testing`: TestContainers.PostgreSql-based integration tests for EF Core and real database behavior
- `api-testing`: ASP.NET Core integration testing via WebApplicationFactory for HTTP endpoint verification

### Modified Capabilities
None — existing Modules 1–3 code has no specs yet. Tests are written retroactively.

## Approach

**Stack**: xUnit 2.9.x, Moq 4.20.x, coverlet 6.x, TestContainers.PostgreSql 4.3.0, `Microsoft.AspNetCore.Mvc.Testing`.

**Database strategy**: TestContainers.PostgreSql (programmatic, isolated) as primary. Docker Compose PostgreSQL as fallback. Separate `dinder_test` database to avoid dev data corruption.

**Teaching sequence** (4 sessions):
1. Unit tests — `HealthCheckQueryHandler` (Arrange-Act-Assert, `[Fact]`, `async Task`)
2. Entity tests — `User` defaults, property assignment, EF model validation
3. Integration tests — DbContext with TestContainers, CRUD round-trip, unique constraint
4. API tests — `WebApplicationFactory`, HTTP assertions, JSON response verification

**Naming**: `MethodName_Scenario_ExpectedBehavior`.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `tests/Dinder.UnitTests/` | New | xUnit project referencing Domain, Application |
| `tests/Dinder.IntegrationTests/` | New | xUnit project referencing Api, Infrastructure |
| `Dinder.slnx` | Modified | Add 2 test projects to solution |
| `docker-compose.yml` | Modified | Add `dinder_test` connection string |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Docker not running | Medium | Unit tests run first (no Docker); document `docker compose up -d` prerequisite |
| EF InMemory false confidence | High | Forbid InMemory for integration tests; real PostgreSQL only |
| Test/Dev DB collision | High | Separate `dinder_test` database or TestContainers isolation |
| Concept overload | Medium | Strict 2-3 test files per session; incremental complexity |

## Rollback Plan

Delete `tests/` directory and revert `Dinder.slnx` changes. No production code modified.

## Dependencies

- Docker Desktop running for integration tests
- Existing `docker-compose.yml` PostgreSQL (fallback path)

## Success Criteria

- [ ] `dotnet test` passes all tests from both projects
- [ ] Code coverage report generated via coverlet (threshold: 0%, informational only)
- [ ] Integration tests run against real PostgreSQL (not InMemory)
- [ ] User wrote every test file manually (no AI-generated test code)
- [ ] 4 teaching sessions completed, each introducing one testing concept
