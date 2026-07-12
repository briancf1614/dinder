# Delta for Gateway Routing

## MODIFIED Requirements

### Requirement: nginx Routes Requests to Backend Services

nginx MUST route requests by URL path. nginx MUST listen on port 80.
(Previously: port 8080)

#### Scenario: API health through gateway

- GIVEN nginx on port 80
- WHEN request at `/api/health`
- THEN proxied to `http://dinder-api:5022/health`

#### Scenario: Auth endpoints through gateway

- GIVEN nginx on port 80
- WHEN request at `/api/auth/register`
- THEN proxied to `http://dinder-api:5022/auth/register`

#### Scenario: Health service through gateway

- GIVEN nginx on port 80
- WHEN request at `/health`
- THEN proxied to `http://health-service:5001/health`

### Requirement: Gateway Is the Only Public Entry Point

Only nginx MUST be exposed on port 80. API and Health MUST be internal-only.
(Previously: nginx exposed on port 8080)

#### Scenario: Direct API access blocked

- GIVEN docker-compose running
- WHEN accessing `http://localhost:5022`
- THEN connection refused

#### Scenario: Direct health service blocked

- GIVEN docker-compose running
- WHEN accessing `http://localhost:5001`
- THEN connection refused

#### Scenario: Gateway access succeeds

- GIVEN docker-compose running
- WHEN accessing `http://localhost:80/api/health`
- THEN request succeeds through nginx
