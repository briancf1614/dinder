# Gateway Routing Specification

## Purpose

Specify nginx reverse proxy routing behavior for the Dinder architecture. The gateway MUST be the only public entry point. All backend services MUST be accessible only through the internal Docker network.

## Requirements

### Requirement: nginx Routes Requests to Backend Services

The nginx reverse proxy MUST route incoming requests to the correct backend based on the URL path.

#### Scenario: API health endpoint through gateway

- GIVEN nginx is running on port 8080
- WHEN a request arrives at `/api/health`
- THEN nginx proxies the request to `http://dinder-api:5022/health` and returns the response

#### Scenario: Auth endpoints through gateway

- GIVEN nginx is running on port 8080
- WHEN a request arrives at `/api/auth/register`
- THEN nginx proxies the request to `http://dinder-api:5022/auth/register` with the original body and headers

#### Scenario: Health service through gateway

- GIVEN nginx is running on port 8080
- WHEN a request arrives at `/health`
- THEN nginx proxies the request to `http://health-service:5001/health` and returns the JSON response

### Requirement: Gateway Is the Only Public Entry Point

Only nginx MUST be exposed to the host machine. The API and Health services MUST be accessible only through the internal Docker network.

#### Scenario: Direct API access is blocked

- GIVEN docker-compose is running
- WHEN accessing `http://localhost:5022` directly from the host
- THEN the connection is refused (port not exposed)

#### Scenario: Direct health service access is blocked

- GIVEN docker-compose is running
- WHEN accessing `http://localhost:5001` directly from the host
- THEN the connection is refused (port not exposed)

#### Scenario: API access through gateway succeeds

- GIVEN docker-compose is running
- WHEN accessing `http://localhost:8080/api/health`
- THEN the request succeeds through nginx
