# Delta for Docker Compose

## MODIFIED Requirements

### Requirement: All Services Run Together

`docker compose up` MUST start nginx, Dinder API, Health Service, and PostgreSQL. Services MUST communicate via container names on an internal bridge network. PostgreSQL MUST NOT expose its port to the host. All services MUST have `restart: unless-stopped`. API and Health MUST have `healthcheck` stanzas. Dinder API MUST run with `ASPNETCORE_ENVIRONMENT=Production`. nginx port MUST be `80:80`.
(Previously: no restart, no healthchecks, PostgreSQL exposed, nginx at 8080, no production env)

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
(Previously: gateway on port 8080)

#### Scenario: Registration returns JWT

- GIVEN all services running
- WHEN `POST http://localhost:80/api/auth/register` with valid payload
- THEN response is `200 OK` with JWT token
