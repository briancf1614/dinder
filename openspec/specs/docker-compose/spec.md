# Docker Compose Specification

## Purpose

Specify the Docker Compose orchestration for all Dinder services. All services MUST communicate via container names on an internal bridge network. The existing authentication flow MUST work through the gateway.

## Requirements

### Requirement: All Services Run Together

`docker compose up` MUST start all four services: nginx, Dinder API, Health Service, and PostgreSQL. Services MUST communicate via container names on an internal bridge network.

#### Scenario: All containers start successfully

- GIVEN `docker compose up` completes
- WHEN checking container status
- THEN all 4 containers are running

### Requirement: API Database Works Through Gateway

The existing register/login/me flow MUST work when accessed through the gateway.

#### Scenario: Registration through gateway returns JWT

- GIVEN all services are running
- WHEN `POST http://localhost:8080/api/auth/register` with valid payload
- THEN response is `200 OK` with JWT token
