# Health Service Specification

## Purpose

Specify a minimal .NET health-check service that operates independently of all other services. The health service MUST start and respond without any external dependencies.

## Requirements

### Requirement: Health Service Returns Status

A minimal .NET service MUST expose a single endpoint that returns service status information.

#### Scenario: Health endpoint returns 200 with status JSON

- GIVEN the health service is running
- WHEN `GET /health` is called
- THEN the response is `200 OK` with JSON body `{ "service": "health", "status": "ok" }`

### Requirement: Health Service Has Zero External Dependencies

The health service MUST NOT depend on PostgreSQL, any NuGet package beyond ASP.NET Core, or any other service. It MUST start and respond independently.

#### Scenario: Health service works when PostgreSQL is down

- GIVEN the health service is running but PostgreSQL is down
- WHEN `GET /health` is called
- THEN the response is still `200 OK`
