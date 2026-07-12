## Exploration: Deploy Dinder to Oracle ARM Server

### Current State

Dinder is a .NET 10 Clean Architecture dating app running locally via Docker Compose:

- **4 services**: postgres (17-alpine), dinder-api (port 5022 internal), health-service (port 5001 internal), nginx (port 8080 host)
- **Dockerfiles**: Both API and Health use `mcr.microsoft.com/dotnet/sdk:10.0` / `aspnet:10.0` multi-arch tags — `docker build` resolves to the host's native arch automatically
- **Secrets**: `.env` file with `POSTGRES_PASSWORD` and `JWT_SECRET` — gitignored, never committed
- **No CI/CD pipeline** exists — no `.github/workflows/`
- **nginx.conf**: Minimal reverse proxy — no TLS, no rate limiting, no security headers
- **PostgreSQL port 5432** is exposed to the host (production risk)
- **Target server**: Oracle Cloud free tier ARM (Ampere A1), 6GB RAM, 50GB disk, Ubuntu, ARM64

### Affected Areas

- `.github/workflows/deploy.yml` — **new**: GitHub Actions SSH-based deployment workflow
- `docker-compose.yml` — production hardening: remove PostgreSQL port exposure, switch nginx port 8080→80, add health checks
- `docker-compose.prod.yml` — **new**: production-specific overrides (alternative to modifying existing)
- `nginx.conf` — production hardening: security headers, TLS preparation, rate limiting stubs
- `.env.example` — may need `ASPNETCORE_ENVIRONMENT=Production` guidance
- `src/Dinder.Health/Dockerfile` — fragile path resolution (context is `./src/Dinder.Health`; works but implicit `.` project discovery)
- `Dinder.slnx` — no direct changes; deploy scripts reference solution for `dotnet test` in CI

### Approaches

#### 1. SSH Deploy — Build on Server with GitHub Actions Orchestration

GitHub Actions workflow SSHs into the Oracle ARM server, pulls the repo, runs `docker compose build` (native ARM64), and restarts services.

**Flow:**
```
GitHub Actions (push to main)
  → SSH to Oracle ARM
  → git pull
  → scp .env from GitHub Secrets
  → docker compose build (native arm64, ~2-5 min)
  → docker compose up -d --force-recreate
```

- **Pros**:
  - Native ARM64 builds — zero emulation, fast (2-5 min for .NET 10 publish)
  - Minimal pipeline complexity — a single workflow file, no registry, no QEMU
  - Server is self-contained — no dependency on Docker Hub/GHCR availability
  - Matches what user is doing now manually (Docker + git on server)
- **Cons**:
  - Server needs Docker + Docker Compose plugin + git installed (user doing this now)
  - Build happens on production server — consumes CPU/RAM during deploy (~1-2GB peak)
  - Limited CI visibility — build logs only via SSH output in Actions
  - No immutable image artifact — hard to rollback to exact previous build
- **Effort**: Low

#### 2. CI Build with QEMU Multi-Arch + Registry Push + Remote Pull

GitHub Actions x86 runner uses `docker/setup-qemu-action` to emulate ARM64, builds multi-arch images via `docker buildx`, pushes to GHCR, then SSH deploys by pulling pre-built images.

**Flow:**
```
GitHub Actions (push to main)
  → setup QEMU + buildx
  → docker buildx build --platform linux/arm64
  → push to ghcr.io/{user}/dinder-api, ghcr.io/{user}/dinder-health
  → SSH to Oracle ARM
  → docker compose pull
  → docker compose up -d
```

- **Pros**:
  - Immutable image artifacts in GHCR — rollback is just `docker pull <old-tag>`
  - CI build visibility — full logs, timing, caching in GitHub Actions
  - Server only needs Docker runtime (no .NET SDK, no build deps)
  - Production server not taxed during build
- **Cons**:
  - QEMU emulation for `dotnet publish` is **very slow** (20-60 minutes for a .NET 10 solution)
  - Buildx + multi-arch caching adds complexity (`docker/build-push-action`)
  - Requires GHCR authentication on both CI (push) and server (pull)
  - GHCR free tier has storage/bandwidth limits (2GB, though likely sufficient)
  - If GHCR is down, deploy is blocked
- **Effort**: High

#### 3. Self-Hosted GitHub Actions Runner on ARM Server

Install a self-hosted GitHub Actions runner on the Oracle ARM server. The runner executes jobs natively on ARM64 — build happens on the server but with full Actions logging.

**Flow:**
```
GitHub Actions (push to main)
  → job dispatched to self-hosted ARM runner
  → git checkout (actions/checkout)
  → docker compose build (native arm64)
  → docker compose up -d
```

- **Pros**:
  - Native ARM64 builds with full CI visibility and logging
  - Secrets management via GitHub Secrets (no SSH credential needed)
  - Same server, same Docker — simple mental model
  - Free for public repos
- **Cons**:
  - Security risk: self-hosted runner on production server with repo access
  - Runner must stay online 24/7 or Actions jobs queue forever
  - Runner maintenance (updates, restart if crashed)
  - Overkill for a learning project with single-server deployment
- **Effort**: Medium

### Supplementary Concerns (all approaches)

| Concern | Analysis |
|---------|----------|
| **Secrets management** | `.env` is gitignored. Must be created on server. Write via `echo` over SSH or `scp` from GitHub Secrets. Only 2 secrets needed: `POSTGRES_PASSWORD`, `JWT_SECRET`. |
| **Zero-downtime** | `docker compose up -d` with `--force-recreate` stops containers briefly (~5-15s). Acceptable for a learning project. Blue-green via Traefik/Caddy or Nginx upstream switching is overkill here. |
| **Health checks** | Add Docker `healthcheck` to `services.api` (hitting `/api/health`) and `services.health` (hitting `/health`). Nginx already has both endpoints. |
| **PostgreSQL exposure** | Port `5432:5432` exposes DB to the world on Oracle Cloud (if Security List allows). Remove the host port mapping entirely — keep DB internal to `dinder-net`. Add a pgAdmin or `docker exec` for admin. |
| **Nginx port** | Currently `8080:80`. For production, change to `80:80` (or `443:443` + TLS later). Oracle Cloud Security Lists must allow port 80. |
| **Rollback** | Approach 1: `git checkout <previous-commit>` + rebuild. Approach 2: `docker pull <old-image-tag>` + restart. Approach 3: same as 1. |
| **Oracle Cloud firewall** | Oracle calls them "Security Lists." Must add ingress rule for port 80 (and 443 if TLS added) to subnet's security list. SSH port 22 should already be open from setup. |

### Recommendation

**Approach 1 — SSH Deploy with Build on Server** — is the right choice for this project.

**Why**:
1. It's the simplest path that works reliably on ARM64 — no QEMU pain, no registry overhead
2. The user is already installing Docker + git on the server — this approach builds on what's already in motion
3. For a learning project with a single server, the immutability/rollback benefits of a registry don't outweigh the 10x build time penalty of QEMU emulation
4. A single `deploy.yml` workflow file is all we need — the rest is production hardening of existing configs
5. If the project outgrows this approach (multiple servers, need for immutable artifacts), migrating to Approach 2 later is straightforward — the Dockerfiles already support multi-arch

**Production hardening should include**:
- Remove PostgreSQL host port exposure or bind to `127.0.0.1:5432:5432`
- Switch nginx from `8080:80` to `80:80`
- Add Docker Compose `healthcheck` stanzas for api and health services
- Add `restart: unless-stopped` to all services
- Add `ASPNETCORE_ENVIRONMENT=Production` to API service

### Risks

- **PostgreSQL data persistence**: `pgdata` volume survives `docker compose down` but NOT `docker compose down -v`. Must document this — accidental `-v` flag wipes the database.
- **`.env` gets out of sync**: If new secrets are added later, the server's `.env` won't update automatically. The deploy script must handle this (overwrite vs. merge).
- **Oracle Cloud free tier limits**: 50GB disk — Docker images + PostgreSQL data could fill it. Monitor disk usage. ARM instances can be reclaimed by Oracle if idle.
- **Docker build on 6GB RAM**: `dotnet publish` with multiple projects could OOM. The build uses `COPY . .` and publishes the whole solution — may need `--no-restore` optimization or use solution-level Dockerfile.
- **No TLS**: nginx currently serves plain HTTP. This is fine for initial deploy but MUST be addressed before any real user data. Certbot + Let's Encrypt later.

### Ready for Proposal

Yes — the exploration is complete. The orchestrator should proceed to `sdd-propose` for `module7-deploy` with these findings. The proposal should scope two phases: (1) the GitHub Actions SSH deploy pipeline, and (2) production hardening of docker-compose and nginx config.
