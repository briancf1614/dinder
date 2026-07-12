# Docker Compose Specification

## Purpose

Specify the Docker Compose orchestration for all Dinder services. All services MUST communicate via container names on an internal bridge network. The existing authentication flow MUST work through the gateway.

## Requirements

### Requirement: All Services Run Together

`docker compose up` MUST start nginx, Dinder API, Health Service, and PostgreSQL. Services MUST communicate via container names on an internal bridge network. PostgreSQL MUST NOT expose its port to the host. All services MUST have `restart: unless-stopped`. API and Health MUST have `healthcheck` stanzas. Dinder API MUST run with `ASPNETCORE_ENVIRONMENT=Production`. nginx port MUST be `80:80`.

#### Scenario: All containers start

- GIVEN `docker compose up` completes
- WHEN checking status
- THEN all 4 containers run with `restart: unless-stopped`

#### Scenario: PostgreSQL is internal only

- GIVEN docker-compose running
- WHEN checking host ports
- THEN PostgreSQL 5432 is not accessible from the host

#### Scenario: Healthcheck failure triggers restart

- GIVEN docker-compose running
- WHEN an API or Health healthcheck fails
- THEN Docker marks it unhealthy and `unless-stopped` handles recovery

### Requirement: API Database Works Through Gateway

The register/login/me flow MUST work through the gateway on port 80.

#### Scenario: Registration returns JWT

- GIVEN all services running
- WHEN `POST http://localhost:80/api/auth/register` with valid payload
- THEN response is `200 OK` with JWT token
