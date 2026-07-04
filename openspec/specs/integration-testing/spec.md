# Integration Testing Specification

## Purpose

Specify integration tests that SHALL run against a real PostgreSQL instance via TestContainers. Tests MUST NOT run against the development database. Database isolation SHALL be achieved through programmatic container lifecycle (TestContainers.PostgreSql).

## Requirements

### Requirement: DbContext Connectivity and Schema Creation

The `DinderDbContext` MUST successfully connect to a PostgreSQL container and create the database schema via `EnsureCreated()` or `Database.Migrate()`.

#### Scenario: DbContext connects and creates schema without error

- GIVEN a running PostgreSQL container from TestContainers
- WHEN `DinderDbContext` is instantiated with the container's connection string and `EnsureCreated()` is called
- THEN no exception SHALL be thrown
- AND the database SHALL contain the `Users` table

### Requirement: User CRUD Round-Trip Preserves Data Integrity

Saving a `User` entity and retrieving it MUST return a `User` with identical property values. This SHALL verify the complete save-and-retrieve pipeline.

#### Scenario: Save and retrieve User maintains all properties

- GIVEN a `DinderDbContext` connected to a running PostgreSQL container
- WHEN a `User` with known `Id`, `Email`, `PasswordHash`, and `CreatedAt` is saved via `Add` + `SaveChanges`, and then retrieved via `Find`
- THEN the retrieved `User` MUST have the same `Id`
- AND the same `Email`
- AND the same `PasswordHash`

### Requirement: Unique Email Constraint Enforcement

The database MUST reject insertion of a second `User` with a duplicate email. A `DbUpdateException` (or inner `PostgresException`) SHALL be thrown.

#### Scenario: Duplicate email throws DbUpdateException

- GIVEN a `User` saved with `Email = "duplicate@test.com"`
- WHEN a second `User` with the same email is saved via `Add` + `SaveChanges`
- THEN a `DbUpdateException` MUST be thrown

### Requirement: Migration Application to Fresh PostgreSQL

The `InitialCreate` migration MUST be successfully applicable to a fresh PostgreSQL database via `Database.Migrate()`, creating all expected tables and constraints.

#### Scenario: InitialCreate migration applies without error

- GIVEN a running PostgreSQL container with an empty database
- WHEN `Database.Migrate()` is called on a `DinderDbContext` connected to that container
- THEN no exception SHALL be thrown
- AND the `Users` table MUST exist with columns `Id`, `Email`, `PasswordHash`, `CreatedAt`
