# Design: Module 4 — Testing

## Technical Approach

Two test projects following the proven old-codebase pattern: `tests/Dinder.UnitTests` (xUnit + Moq) and `tests/Dinder.IntegrationTests` (xUnit + TestContainers + WebApplicationFactory). Tests are retroactive — validating existing Modules 1–3 behavior. No production code changes except `InternalsVisibleTo` on Dinder.Api and `Dinder.slnx` additions.

**Stack**: xUnit 2.9.3, Moq 4.20.72, Testcontainers.PostgreSql 4.3.0, coverlet 6.0.4, `Microsoft.AspNetCore.Mvc.Testing`.

## Architecture Decisions

### AD-1: TestContainers vs Docker Compose for integration DB

| Option | Tradeoff |
|--------|----------|
| **TestContainers (chosen)** | Programmatic lifecycle, isolated per-run, no port conflicts, no compose dependency |
| Docker Compose `dinder_test` DB | Manual `docker compose up` prerequisite, port 5432 collision risk, shared state |

**Rationale**: TestContainers removes the "Docker not running" footgun from CI mindset. Container spins up/down per test run — zero state leakage. The old codebase declared this dependency but never used it; Module 4 rectifies that.

### AD-2: xUnit Collection Fixture for TestContainers

| Option | Tradeoff |
|--------|----------|
| **Collection Fixture (chosen)** | Single container per test run, shared across all integration test classes, ~5s startup once |
| Per-test container | Isolation extreme but 5s startup per test, unacceptable for 6+ scenarios |
| Class Fixture | One container per test class, 3 containers for 3 classes — wasteful |

**Rationale**: Collection Fixture gives serial execution (safe for single DB) with minimal overhead. The `[CollectionDefinition("Database")]` + `ICollectionFixture<DatabaseFixture>` pattern is the xUnit convention.

### AD-3: InternalsVisibleTo for WebApplicationFactory

**Choice**: Add `<InternalsVisibleTo Include="Dinder.IntegrationTests" />` to `Dinder.Api.csproj`.
**Rationale**: `Program.cs` uses top-level statements — the generated `Program` class is `internal`. Without this, `WebApplicationFactory<Program>` fails at compile time. This is the documented .NET approach and a one-line change.

### AD-4: No docker-compose changes

**Choice**: Leave `docker-compose.yml` as-is. TestContainers manages its own PostgreSQL instance programmatically — no compose service needed for tests. Dev database stays isolated.

## Data Flow

```
Integration test run
        │
        ▼
[Collection Fixture] ──starts──▶ TestContainers.PostgreSql ◀──conn string──▶ DinderDbContext
        │                                                                         │
        │  shared across all [Collection("Database")] tests                       │
        ▼                                                                         ▼
[MigrationTests] ──Migrate()──▶ Users table                              [DbContextTests]
                                                                         CRUD / constraints
[WebApplicationFactory<Program>]
  overrides conn string ──▶ TestContainers DB
        │
        ▼
[HealthEndpointTests / RootEndpointTests]
  HttpClient ──HTTP──▶ test server ──▶ handler ──▶ response
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Dinder.slnx` | Modify | Add `/tests/` folder with both test projects |
| `src/Dinder.Api/Dinder.Api.csproj` | Modify | Add `InternalsVisibleTo` for `Dinder.IntegrationTests` |
| `tests/Dinder.UnitTests/Dinder.UnitTests.csproj` | Create | xUnit + Moq + coverlet. References Domain, Application, Infrastructure |
| `tests/Dinder.UnitTests/HealthCheckQueryHandlerTests.cs` | Create | 2 tests: returns healthy status, returns recent timestamp (~30 lines) |
| `tests/Dinder.UnitTests/HealthCheckResultTests.cs` | Create | 1 test: default Status="" and Timestamp=DateTime.MinValue (~18 lines) |
| `tests/Dinder.UnitTests/UserEntityTests.cs` | Create | 2 tests: default construction, property assignment (~30 lines) |
| `tests/Dinder.UnitTests/DinderDbContextConfigurationTests.cs` | Create | 4 tests: Email MaxLength 256, Email unique index, Id PK, PasswordHash required (~55 lines) |
| `tests/Dinder.IntegrationTests/Dinder.IntegrationTests.csproj` | Create | xUnit + TestContainers + Mvc.Testing + coverlet. References Api, Infrastructure |
| `tests/Dinder.IntegrationTests/DatabaseCollection.cs` | Create | Collection Fixture: starts PostgreSql container, exposes connection string (~45 lines) |
| `tests/Dinder.IntegrationTests/DbContextTests.cs` | Create | 3 tests: EnsureCreated, CRUD round-trip, unique email violation (~55 lines) |
| `tests/Dinder.IntegrationTests/MigrationTests.cs` | Create | 1 test: Database.Migrate() on fresh PostgreSQL (~30 lines) |
| `tests/Dinder.IntegrationTests/HealthEndpointTests.cs` | Create | CustomWebApplicationFactory + 1 test: GET /health → 200 + JSON (~45 lines) |
| `tests/Dinder.IntegrationTests/RootEndpointTests.cs` | Create | 1 test: GET / → 200 + text body (~22 lines) |

## Interfaces / Contracts

### DatabaseCollection.cs — Fixture contract
```csharp
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

public class DatabaseFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; private set; }
    public string ConnectionString => Container.GetConnectionString();
    public async Task InitializeAsync() { /* start container */ }
    public async Task DisposeAsync() { /* stop container */ }
}
```

### HealthEndpointTests.cs — WebApplicationFactory override
```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    public CustomWebApplicationFactory(string connectionString) { _connectionString = connectionString; }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DbContext registration with TestContainers connection string
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<DinderDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<DinderDbContext>(opts => opts.UseNpgsql(_connectionString));
        });
    }
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (no I/O) | Handler logic, model defaults, entity properties | xUnit `[Fact]`, Arrange-Act-Assert, zero dependencies |
| Unit (EF config) | Fluent API configuration via `IModel` inspection | Build model from `DbContextOptionsBuilder`, inspect `IModel` metadata |
| Integration (DB) | DbContext connectivity, CRUD, constraints, migrations | TestContainers + Collection Fixture, real PostgreSQL |
| Integration (API) | HTTP endpoint responses, JSON shape, status codes | `WebApplicationFactory<Program>` with overridden connection string |

## Migration / Rollout

No data migration required. Rollback: delete `tests/` directory, revert `Dinder.slnx`, remove `InternalsVisibleTo` from `Dinder.Api.csproj`.

## Open Questions

- [ ] Confirm Docker Desktop is installed and working on user's machine before Session 3 (integration tests)
- [ ] User preference: `dotnet test` with `--filter` or run entire suite?
