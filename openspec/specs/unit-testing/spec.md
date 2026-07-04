# Unit Testing Specification

## Purpose

Specify xUnit-based unit tests for domain and application layers. Tests SHALL run without I/O — no database, no HTTP, no filesystem. Tests MUST use `MethodName_Scenario_ExpectedBehavior` naming.

## Requirements

### Requirement: HealthCheckQueryHandler Produces Healthy Result

The handler MUST return a `HealthCheckResult` with `Status = "healthy"` and a `Timestamp` within a reasonable tolerance of `DateTime.UtcNow`. The handler SHALL return a completed `Task<HealthCheckResult>`.

#### Scenario: Handler returns healthy status and recent timestamp

- GIVEN a new `HealthCheckQueryHandler` instance
- WHEN `Handle` is called with a default `HealthCheckQuery`
- THEN the result Status MUST equal `"healthy"`
- AND the result Timestamp MUST be within 5 seconds of `DateTime.UtcNow`

### Requirement: HealthCheckResult Default Values

A newly constructed `HealthCheckResult` MUST initialize `Status` to `string.Empty` and `Timestamp` to `DateTime.MinValue` — matching C# default behavior for reference and value types.

#### Scenario: Default construction yields empty/default values

- GIVEN a `new HealthCheckResult()` with no property assignments
- THEN `Status` MUST equal `string.Empty`
- AND `Timestamp` MUST equal `DateTime.MinValue`

### Requirement: User Entity Default Construction

A default `User` instance MUST have `Id = Guid.Empty`, `Email = string.Empty`, `PasswordHash = string.Empty`, and `CreatedAt = DateTime.MinValue`.

#### Scenario: New User has expected defaults

- GIVEN a `new User()` with no property assignments
- THEN `Id` SHALL equal `Guid.Empty`
- AND `Email` SHALL equal `string.Empty`
- AND `PasswordHash` SHALL equal `string.Empty`
- AND `CreatedAt` SHALL equal `DateTime.MinValue`

### Requirement: User Entity Property Assignment

All four properties of `User` (`Id`, `Email`, `PasswordHash`, `CreatedAt`) MUST be independently settable to non-default values.

#### Scenario: Setting all properties persists values

- GIVEN a `new User()`
- WHEN `Id` is set to a known `Guid`, `Email` to `"test@example.com"`, `PasswordHash` to `"hash123"`, and `CreatedAt` to a known `DateTime`
- THEN each property getter MUST return the assigned value

### Requirement: EF Core Model Validation — User Configuration

The `DinderDbContext` OnModelCreating MUST configure the `User` entity with: `HasKey(u => u.Id)`, `Email` with `HasMaxLength(256)` and `IsRequired`, a unique index on `Email`, and `PasswordHash` with `IsRequired`. These SHALL be verifiable by building an `IModel` from `DbContextOptions<DinderDbContext>` in memory.

#### Scenario: Email column has max length 256

- GIVEN an `IModel` built from `DinderDbContext`
- WHEN inspecting the `User` entity's `Email` property
- THEN `GetMaxLength()` MUST return `256`

#### Scenario: Email index is unique

- GIVEN an `IModel` built from `DinderDbContext`
- WHEN inspecting the index on `Email`
- THEN `IsUnique` MUST be `true`

#### Scenario: Id is primary key

- GIVEN an `IModel` built from `DinderDbContext`
- WHEN inspecting the `User` entity's `Id` property
- THEN `IsPrimaryKey()` MUST return `true`
