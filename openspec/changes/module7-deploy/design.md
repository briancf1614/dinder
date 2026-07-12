# Design: Deploy Dinder to Production (Oracle ARM)

## Technical Approach

GitHub Actions SSH-based deploy on push to `main`. The Oracle ARM server builds Docker images natively (no QEMU), then restarts services. Production hardening is achieved through modifications to `docker-compose.yml` and `nginx.conf` — no separate production compose file (simpler mental model for a single-server learning project).

## Architecture Decisions

| # | Decision | Choice | Alternatives | Rationale |
|---|----------|--------|-------------|-----------|
| 1 | **Deploy method** | SSH + build-on-server | QEMU multi-arch + registry, self-hosted runner | Native ARM64 builds are fast (~2-5 min). Registry adds QEMU emulation overhead (20-60 min for .NET publish). Single server doesn't need immutable artifacts yet. |
| 2 | **SSH Action** | `appleboy/ssh-action@v1` | `ssh` CLI in bash, custom composite action | Well-maintained (4k+ stars), handles key loading and host key checking, supports `env:` passthrough for secrets. |
| 3 | **Secrets flow** | `env:` passthrough + `printf` to `.env` | `scp`, GitHub Environments, `echo >> .env` | `env:` in ssh-action passes secrets as remote env vars. `printf` writes them without echoing to logs. `echo "SECRET=$SECRET"` risks log leakage if `set -x` is active. |
| 4 | **Compose strategy** | Modify single `docker-compose.yml` | Separate `docker-compose.prod.yml` overlay | One file to reason about. Overlay adds indirection that buys nothing for a single-server project. If multi-env needed later, split is trivial. |

## Data Flow

```
GitHub push to main
    │
    ▼
GitHub Actions (ubuntu-latest)
    │  secrets.SSH_HOST, SSH_USER, SSH_KEY
    ▼
Oracle ARM server (SSH)
    │  git pull
    │  printf POSTGRES_PASSWORD + JWT_SECRET → .env
    │  docker compose build (native ARM64)
    │  docker compose up -d --force-recreate
    ▼
    ┌──────────────────────────────────────┐
    │  nginx :80 ← api :5022              │
    │          ← health :5001             │
    │          postgres :5432 (internal)  │
    └──────────────────────────────────────┘
```

Secrets never touch the Actions runner's filesystem — only transmitted as `env:` variables over SSH and written directly to `.env` on the server.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `.github/workflows/deploy.yml` | **Create** | SSH deploy workflow: check secrets → SSH → git pull → write .env → build → up |
| `docker-compose.yml` | **Modify** | Remove `ports: 5432:5432` from postgres. Change nginx `8080:80` to `80:80`. Add `restart: unless-stopped` to all 4 services. Add `healthcheck` to api and health. Add `ASPNETCORE_ENVIRONMENT=Production` to api. |
| `nginx.conf` | **Modify** | Add `access_log` and `error_log` directives for production observability. Listen port already `80` — no change needed. |

## docker-compose.yml Diff (Logical)

```yaml
# postgres: REMOVE ports entirely (was 5432:5432)
# postgres: ADD restart: unless-stopped
# api:      ADD restart: unless-stopped
# api:      ADD ASPNETCORE_ENVIRONMENT=Production
# api:      ADD healthcheck (test: curl -f http://localhost:5022/health, interval 30s)
# health:   ADD restart: unless-stopped
# health:   ADD healthcheck (test: curl -f http://localhost:5001/health, interval 30s)
# nginx:    CHANGE ports: "80:80" (was "8080:80")
# nginx:    ADD restart: unless-stopped
```

## nginx.conf Changes

Add after `http {`:
```nginx
access_log /var/log/nginx/access.log;
error_log  /var/log/nginx/error.log warn;
```

Existing `listen 80;` and `location` blocks unchanged — they already route correctly.

## Secrets Contract

| Secret | GitHub Name | Written To | Used By |
|--------|------------|------------|---------|
| Server IP | `SSH_HOST` | — | ssh-action `host:` |
| SSH user | `SSH_USER` | — | ssh-action `username:` |
| SSH private key | `SSH_KEY` | — | ssh-action `key:` |
| DB password | `POSTGRES_PASSWORD` | `.env` | docker-compose (postgres + api) |
| JWT signing key | `JWT_SECRET` | `.env` | docker-compose (api) |

`.env` is gitignored (`*.env` in `.gitignore`). Written fresh on every deploy — no merge, no drift.

## Security Considerations

- **SSH key**: Stored as GitHub Secret `SSH_KEY`. Never written to disk on the Actions runner. The `appleboy/ssh-action` loads it into the SSH agent in-memory.
- **Secrets masking**: GitHub automatically masks `${{ secrets.* }}` in logs. The `printf` approach on the remote side avoids `echo $SECRET` which could leak if `bash -x` or `set -x` is ever enabled on the server.
- **PostgreSQL**: Host port `5432` removed. DB is only reachable inside `dinder-net` bridge network. Oracle Security List ingress rule for 5432 not needed.
- **Surface area**: Only nginx on port 80 exposed. API (5022) and Health (5001) are container-internal — no host ports mapped.

## Testing Strategy

| Layer | What | How |
|-------|------|-----|
| **Pre-deploy** | `dotnet test` passes | Run locally; also add `dotnet test` step in workflow before SSH deploy |
| **Post-deploy smoke** | Health endpoints respond | `curl http://<host>/health` and `curl http://<host>/api/health` in workflow as final step |
| **Manual** | Full auth flow | `POST /api/auth/register` → `POST /api/auth/login` → `GET /api/auth/me` (follows docker-compose spec scenario) |
| **Security** | DB port not exposed | `ssh` to server → `docker compose ps` confirms no 5432 host mapping |

## Migration / Rollout

No migration required. This is a greenfield deploy pipeline for a project not yet in production. Rollback: SSH into server, `git checkout <previous-commit>`, `docker compose up -d --force-recreate`. Database volume (`pgdata`) survives rollback.

## Open Questions

- [ ] Oracle Cloud Security List for port 80 must be created manually in OCI console before first deploy (out of CI scope)
- [ ] Confirm Docker Compose *plugin* vs standalone binary on server — affects command: `docker compose` vs `docker-compose`
