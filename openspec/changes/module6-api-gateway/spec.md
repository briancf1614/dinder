# Delta Specs: Module 6 — API Gateway (nginx)

## gateway-routing

### nginx Routes Requests to Backend Services

The nginx reverse proxy MUST route incoming requests to the correct backend based on the URL path.

**Scenario**: GIVEN nginx is running on port 8080; WHEN a request arrives at `/api/health`; THEN nginx proxies the request to `http://dinder-api:5022/health` and returns the response.

**Scenario**: GIVEN nginx is running on port 8080; WHEN a request arrives at `/api/auth/register`; THEN nginx proxies the request to `http://dinder-api:5022/auth/register` with the original body and headers.

**Scenario**: GIVEN nginx is running on port 8080; WHEN a request arrives at `/health`; THEN nginx proxies the request to `http://health-service:5001/health` and returns the JSON response.

### Gateway Is the Only Public Entry Point

Only nginx MUST be exposed to the host machine. The API and Health services MUST be accessible only through the internal Docker network.

**Scenario**: GIVEN docker-compose is running; WHEN accessing `http://localhost:5022` directly from the host; THEN the connection is refused (port not exposed).

**Scenario**: GIVEN docker-compose is running; WHEN accessing `http://localhost:5001` directly from the host; THEN the connection is refused (port not exposed).

**Scenario**: GIVEN docker-compose is running; WHEN accessing `http://localhost:8080/api/health`; THEN the request succeeds through nginx.

---

## health-service

### Health Service Returns Status

A minimal .NET service MUST expose a single endpoint that returns service status information.

**Scenario**: GIVEN the health service is running; WHEN `GET /health` is called; THEN the response is `200 OK` with JSON body `{ "service": "health", "status": "ok" }`.

### Health Service Has Zero External Dependencies

The health service MUST NOT depend on PostgreSQL, any NuGet package beyond ASP.NET Core, or any other service. It MUST start and respond independently.

**Scenario**: GIVEN the health service is running but PostgreSQL is down; WHEN `GET /health` is called; THEN the response is still `200 OK`.

---

## docker-compose

### All Services Run Together

`docker compose up` MUST start all four services: nginx, Dinder API, Health Service, and PostgreSQL. Services MUST communicate via container names on an internal bridge network.

**Scenario**: GIVEN `docker compose up` completes; WHEN checking container status; THEN all 4 containers are running.

### API Database Works Through Gateway

The existing register/login/me flow MUST work when accessed through the gateway.

**Scenario**: GIVEN all services are running; WHEN `POST http://localhost:8080/api/auth/register` with valid payload; THEN response is `200 OK` with JWT token.
