# Proposal: Deploy Dinder to Production (Oracle ARM)

## Intent

Dinder runs only locally. Deploy to an Oracle Cloud ARM server so the API is publicly accessible. This is the capstone module — taking a local learning project to production with a real CI/CD pipeline.

**Learning objectives**: GitHub Actions CI/CD, production Docker hardening, infrastructure-as-config, Oracle Cloud networking.

## Scope

### In Scope
- GitHub Actions SSH deploy workflow: push → build ARM64 native → restart services
- Production-hardened `docker-compose.yml`: remove DB host port, add healthchecks, add restart policies, switch nginx 8080→80, add `ASPNETCORE_ENVIRONMENT=Production`
- `nginx.conf`: port 80, production-ready logging
- `.env` bootstrapping on server from GitHub Secrets
- Oracle Cloud Security List: open ingress port 80

### Out of Scope
- TLS/HTTPS (deferred — Let's Encrypt later)
- Zero-downtime deploys (5-15s downtime is acceptable)
- Container registry (GHCR/Docker Hub)
- Monitoring/alerting

## Capabilities

### New Capabilities
- `ci-cd-deploy`: GitHub Actions SSH-based deployment pipeline that pulls, builds, and restarts services on the ARM64 production server on push to `main`

### Modified Capabilities
- `docker-compose`: remove PostgreSQL host port exposure, add `healthcheck` stanzas for api/health services, add `restart: unless-stopped` to all services, switch nginx port mapping from `8080:80` to `80:80`, add `ASPNETCORE_ENVIRONMENT=Production`
- `gateway-routing`: nginx listen port and host mapping changes from 8080 to 80 (production standard)

## Approach

GitHub Actions workflow on push to `main`: SSH into Oracle ARM → `git pull` → write `.env` from GitHub Secrets → `docker compose build` (native ARM64, ~2-5 min) → `docker compose up -d --force-recreate`. No QEMU, no registry — the server builds what it runs.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `.github/workflows/deploy.yml` | New | SSH-based deploy workflow |
| `docker-compose.yml` | Modified | Port security, healthchecks, restart policies, env |
| `nginx.conf` | Modified | Port 80, production logging |
| Oracle Cloud Security List | New | Open ingress port 80 |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Build OOM on 6GB RAM | Low | .NET 10 publish is memory-efficient on ARM; monitor first deploy |
| Secrets in CI logs | Med | GitHub Secrets; `echo` with suppressed output to write `.env` |
| `docker compose down -v` wipes DB | Low | Document warning; volume survives normal `down` |
| Oracle reclaims idle free instance | Low | Acceptable for learning project |

## Rollback Plan

SSH into server → `git checkout <previous-commit>` → `docker compose up -d --force-recreate`. Database volume (`pgdata`) survives rollback — no data loss.

## Dependencies

- Docker + Docker Compose plugin + git on server (user installing now)
- GitHub repo secrets: `SSH_HOST`, `SSH_USER`, `SSH_KEY`, `POSTGRES_PASSWORD`, `JWT_SECRET`
- Oracle Security List: port 80 open (manual OCI console step)

## Success Criteria

- [ ] Push to `main` triggers deploy; all 4 services healthy within 5 min
- [ ] `http://<server-ip>/api/health` returns 200 OK
- [ ] `http://<server-ip>/health` returns `{ "service": "health", "status": "ok" }`
- [ ] PostgreSQL port 5432 NOT exposed on host (internal to `dinder-net` only)
- [ ] Services restart automatically if crashed (`restart: unless-stopped`)
