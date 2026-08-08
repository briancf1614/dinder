# Tasks: Module 4 — Testing

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~430 |
| 400-line budget risk | Medium |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (Session 1) → PR 2 (Session 2) → PR 3 (Session 3) → PR 4 (Session 4) |
| Delivery strategy | ask-always |
| Chain strategy | stacked-to-main |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Unit test infrastructure + HealthCheck tests | PR 1 | Base: main. Creates csproj, edits slnx, 3 tests |
| 2 | Entity + EF configuration tests | PR 2 | Base: main (post PR 1). 6 tests, depends on PR 1 csproj |
| 3 | Integration test infrastructure + DB tests | PR 3 | Base: main (post PR 2). TestContainers fixture, 4 tests |
| 4 | API endpoint tests | PR 4 | Base: main (post PR 3). WebApplicationFactory, 2 tests |

## Session 1: Unit Testing Fundamentals

- [ ] 1.1 Create `tests/Dinder.UnitTests/Dinder.UnitTests.csproj` — xUnit + Moq + coverlet, net10.0, references Domain/Application/Infrastructure. **Concepts**: test project anatomy, NuGet refs. **Verify**: `dotnet build tests/Dinder.UnitTests`
- [ ] 1.2 Modify `Dinder.slnx` — add `<Folder Name="/tests/">` with `Dinder.UnitTests` project. **Concepts**: solution structure. **Verify**: `dotnet build Dinder.slnx`
- [ ] 1.3 Create `tests/Dinder.UnitTests/HealthCheckQueryHandlerTests.cs` — 2 [Fact] tests: Status="healthy", Timestamp within 5s of UtcNow. **Concepts**: Arrange-Act-Assert, async tests, [Fact], Assert.Equal. **Verify**: `dotnet test tests/Dinder.UnitTests --filter HealthCheckQueryHandler`
- [ ] 1.4 Create `tests/Dinder.UnitTests/HealthCheckResultTests.cs` — 1 [Fact] test: new instance defaults Status="" and Timestamp=DateTime.MinValue. **Concepts**: default value testing. **Verify**: `dotnet test tests/Dinder.UnitTests --filter HealthCheckResult`

## Session 2: Entity & EF Configuration Tests

- [ ] 2.1 Create `tests/Dinder.UnitTests/UserEntityTests.cs` — 2 [Fact] tests: default construction (Id=Guid.Empty, etc.), property assignment round-trip. **Concepts**: entity testing, Guid/DateTime defaults. **Verify**: `dotnet test tests/Dinder.UnitTests --filter UserEntity`
- [ ] 2.2 Create `tests/Dinder.UnitTests/DinderDbContextConfigurationTests.cs` — 4 [Fact] tests using DbContextOptionsBuilder to inspect IModel: Email MaxLength(256), Email IsUnique, Id IsPrimaryKey, PasswordHash IsRequired. **Concepts**: EF model validation without DB, IModel inspection. **Verify**: `dotnet test tests/Dinder.UnitTests --filter DbContextConfiguration`

## Session 3: Integration Tests with Real PostgreSQL

- [ ] 3.1 Create `tests/Dinder.IntegrationTests/Dinder.IntegrationTests.csproj` — xUnit + TestContainers 4.3.0 + Mvc.Testing + coverlet, net10.0, references Api/Infrastructure. **Concepts**: integration test deps, TestContainers NuGet. **Verify**: `dotnet build tests/Dinder.IntegrationTests`
- [ ] 3.2 Modify `Dinder.slnx` — add `Dinder.IntegrationTests` to `/tests/` folder. **Verify**: `dotnet build Dinder.slnx`
- [ ] 3.3 Create `tests/Dinder.IntegrationTests/DatabaseCollection.cs` — `DatabaseFixture : IAsyncLifetime` starts PostgreSql container, exposes ConnectionString. `[CollectionDefinition("Database")]`. **Concepts**: Collection Fixture, IAsyncLifetime, TestContainers lifecycle. **Verify**: pool starts without error
- [ ] 3.4 Create `tests/Dinder.IntegrationTests/DbContextTests.cs` — 3 [Fact] tests with `[Collection("Database")]`: EnsureCreated() succeeds, User CRUD round-trip, duplicate email throws DbUpdateException. **Concepts**: real-DB integration, constraint testing. **Verify**: `dotnet test tests/Dinder.IntegrationTests --filter DbContext`
- [ ] 3.5 Create `tests/Dinder.IntegrationTests/MigrationTests.cs` — 1 [Fact] test: Database.Migrate() succeeds, Users table has expected columns. **Concepts**: migration testing. **Verify**: `dotnet test tests/Dinder.IntegrationTests --filter Migration`

## Session 4: API Endpoint Tests

- [ ] 4.1 Modify `src/Dinder.Api/Dinder.Api.csproj` — add `<InternalsVisibleTo Include="Dinder.IntegrationTests" />`. **Concepts**: InternalsVisibleTo for top-level statements. **Verify**: `dotnet build src/Dinder.Api`
- [ ] 4.2 Create `tests/Dinder.IntegrationTests/HealthEndpointTests.cs` — `CustomWebApplicationFactory : WebApplicationFactory<Program>` overriding DbContext to TestContainers. 1 test: GET /health → 200, JSON status="healthy" + ISO 8601 timestamp. **Concepts**: WebApplicationFactory, service override, HTTP integration testing. **Verify**: `dotnet test tests/Dinder.IntegrationTests --filter HealthEndpoint`
- [ ] 4.3 Create `tests/Dinder.IntegrationTests/RootEndpointTests.cs` — 1 test: GET / → 200, non-empty body. **Concepts**: smoke-test pattern. **Verify**: `dotnet test tests/Dinder.IntegrationTests --filter RootEndpoint`

## Final Verification

- [ ] Run full suite: `dotnet test` — all 14 tests green.
- [ ] Run coverage: `dotnet test --collect:"XPlat Code Coverage"` — report generated.
