# Design: Module 6 — API Gateway (nginx)

## Technical Approach

nginx in Docker as reverse proxy. Standalone `nginx.conf` mounted as volume. New `Dinder.Health` project — ASP.NET Core Empty with a single endpoint. All orchestrated via docker-compose with internal bridge network.

**Stack additions**: nginx:alpine Docker image (no NuGet packages needed).

## Architecture

```
┌──────────────────────────────────────────────────────┐
│                    Host Machine                      │
│                                                      │
│  localhost:8080 ──────▶┌──────────────────────────┐  │
│                         │  nginx (Reverse Proxy)   │  │
│                         │  Port 8080 (EXPOSED)     │  │
│                         └──────┬──────────┬────────┘  │
│                                │          │           │
│                    /api/* ─────┘          └── /health │
│                         │                      │      │
│           ┌─────────────▼──────┐   ┌──────────▼────┐ │
│           │  Dinder.Api        │   │  Dinder.Health │ │
│           │  Port 5022 (INT)   │   │  Port 5001(INT)│ │
│           └─────────┬──────────┘   └────────────────┘ │
│                     │                                  │
│           ┌─────────▼──────────┐                       │
│           │  PostgreSQL        │                       │
│           │  Port 5432 (INT)   │                       │
│           └────────────────────┘                       │
│                                                        │
│  ─── internal Docker network "dinder-net" ───          │
└──────────────────────────────────────────────────────┘
```

## Architecture Decisions

### AD-1: nginx vs YARP vs Traefik

| Option | Tradeoff |
|--------|----------|
| **nginx (chosen)** | Industry standard. 5MB image. Works with ANY backend stack. Transferable skill. |
| YARP | .NET-only. Less job market value. Heavier (needs .NET runtime). |
| Traefik | Great for containers, but adds complexity. Better as a second gateway later. |

**Rationale**: nginx runs 40% of the internet. Learning it now means you can configure any gateway. The concept (reverse proxy, location blocks, proxy_pass) maps 1:1 to YARP, Traefik, or Envoy.

### AD-2: Health Service as Separate Project

**Choice**: New `src/Dinder.Health/` project, independent from other Dinder projects.
**Rationale**: Must be independently buildable and deployable. Zero references to Domain/Application/Infrastructure. Pure ASP.NET Core. This is how real microservices work — each is its own deployable unit.

### AD-3: Docker Networking

**Choice**: Custom bridge network `dinder-net`. All services on this network. Only nginx exposes a port to the host.
**Rationale**: Production pattern. Internal services are invisible from outside. Service discovery via Docker DNS (container name = hostname).

### AD-4: No .sln File

**Choice**: Continue without a `.sln` file. Rely on docker-compose for orchestration.
**Rationale**: Each project is independently buildable (`dotnet build src/Dinder.Health/`). docker-compose handles multi-service startup. A `.sln` would add ceremony without solving a real problem at this scale.

### AD-5: nginx Config Approach

**Choice**: Single `nginx.conf` file at repo root, mounted as volume in docker-compose.
**Rationale**: Simple, visible, easy to edit. No custom Docker image needed — use the official `nginx:alpine` image and mount the config.

## File Changes

| # | File | Action | Description |
|---|------|--------|-------------|
| 1 | `nginx.conf` | Create | nginx reverse proxy config (routes, proxy_pass) |
| 2 | `src/Dinder.Health/Dinder.Health.csproj` | Create | ASP.NET Core Web project, zero dependencies |
| 3 | `src/Dinder.Health/Program.cs` | Create | Single `/health` endpoint, 25 lines |
| 4 | `src/Dinder.Health/Properties/launchSettings.json` | Create | Port 5001, no HTTPS |
| 5 | `docker-compose.yml` | Modify | Add nginx, health, internal network |
| 6 | `LEARNING-PATH.md` | Modify | Mark Module 6 in progress |

## nginx Config (key parts)

```nginx
events { }

http {
    server {
        listen 80;

        location /health {
            proxy_pass http://health-service:5001;
        }

        location /api/ {
            proxy_pass http://dinder-api:5022/;
        }
    }
}
```

- `proxy_pass http://health-service:5001` — Docker resolves `health-service` to the container IP
- `location /api/` with trailing slash on proxy_pass — strips `/api` prefix before forwarding (configurable)
- No CORS, no caching, no rate limiting in v1 — keep it minimal to teach the core concept

## Health Service Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => new { service = "health", status = "ok" });

app.Run();
```

That's it. 8 lines. No using statements needed (global usings in .NET 10).

## Docker Compose Plan

```yaml
services:
  nginx:
    image: nginx:alpine
    ports: ["8080:80"]
    volumes: ["./nginx.conf:/etc/nginx/nginx.conf:ro"]
    depends_on: [api, health]
    networks: [dinder-net]

  api:
    build: ./src/Dinder.Api
    depends_on: [postgres]
    networks: [dinder-net]
    # NO ports: — internal only

  health:
    build: ./src/Dinder.Health
    networks: [dinder-net]
    # NO ports: — internal only

  postgres:
    # existing config, add network

networks:
  dinder-net:
    driver: bridge
```

Key: `api` and `health` have NO `ports:` mapping — unreachable from host. Only `nginx` exposes `8080:80`.

### Dockerfiles Needed

Both `Dinder.Api` and `Dinder.Health` need `Dockerfile`s. The existing project has none — we build with `dotnet run` locally. For docker-compose we need multi-stage Dockerfiles:

```dockerfile
# src/Dinder.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5022
ENTRYPOINT ["dotnet", "Dinder.Api.dll"]
```

Same pattern for Health (but smaller, no DB context needed).
