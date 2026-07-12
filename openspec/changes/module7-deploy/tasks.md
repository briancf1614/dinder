# Tasks: Deploy Dinder to Production (Oracle ARM)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~100 (1 new file: ~60-80, 2 modified files: ~20) |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

## Phase 1: Production Compose Hardening

- [x] 1.1 Remove `ports: 5432:5432` from postgres service in `docker-compose.yml`
- [x] 1.2 Add `restart: unless-stopped` to postgres, api, health, and nginx services in `docker-compose.yml`
- [x] 1.3 Add `ASPNETCORE_ENVIRONMENT=Production` to api environment in `docker-compose.yml`
- [x] 1.4 Add healthcheck stanza to api service (curl `http://localhost:5022/health`, interval 30s) in `docker-compose.yml`
- [x] 1.5 Add healthcheck stanza to health service (curl `http://localhost:5001/health`, interval 30s) in `docker-compose.yml`
- [x] 1.6 Change nginx port mapping from `"8080:80"` to `"80:80"` in `docker-compose.yml`

## Phase 2: Gateway Production Update

- [x] 2.1 Add `access_log /var/log/nginx/access.log;` after `http {` in `nginx.conf`
- [x] 2.2 Add `error_log /var/log/nginx/error.log warn;` after access_log in `nginx.conf`

## Phase 3: CI/CD Deploy Pipeline

- [x] 3.1 Create `.github/workflows/deploy.yml` — name: "Deploy to Oracle ARM", trigger: push to main
- [x] 3.2 Add pre-deploy step: `dotnet test` (catch regressions before SSH deploy)
- [x] 3.3 Add SSH deploy step via `appleboy/ssh-action@v1` — git pull, write `.env` with `printf` from secrets, `docker compose build`, `docker compose up -d --force-recreate`
- [x] 3.4 Add post-deploy smoke: curl health endpoint to confirm the deploy succeeded

## Phase 4: Manual Steps & Verification

> **NOTE**: Tasks 4.1-4.5 are manual server-side steps that cannot be fully automated or verified in this apply session. They require the Oracle ARM server to be provisioned and accessible.

- [ ] 4.1 Create Oracle Cloud Security List ingress rule for port 80 (manual OCI console step)
  - **Instructions**: Log into Oracle Cloud Console → Networking → Virtual Cloud Networks → Select your VCN → Security Lists → Select the security list attached to your instance's subnet → Add Ingress Rule: Source `0.0.0.0/0`, IP Protocol `TCP`, Destination Port Range `80`, Description `Dinder HTTP ingress`.
- [ ] 4.2 Configure GitHub repo secrets: `SSH_HOST`, `SSH_USER`, `SSH_KEY`, `POSTGRES_PASSWORD`, `JWT_SECRET`
  - **Instructions**: Go to GitHub repo → Settings → Secrets and variables → Actions → New repository secret. Add each of the 5 secrets:
    - `SSH_HOST`: Oracle ARM public IP address
    - `SSH_USER`: SSH username (e.g., `ubuntu` or `opc`)
    - `SSH_KEY`: Private SSH key (contents of `~/.ssh/id_rsa` or equivalent)
    - `POSTGRES_PASSWORD`: Strong password for PostgreSQL
    - `JWT_SECRET`: Minimum 32-character random string for JWT signing
- [ ] 4.3 Push to main, verify workflow passes, all 4 services healthy within 5 min
  - **Verification**: Push a commit to `main`, watch the GitHub Actions run at `https://github.com/<repo>/actions`. After the deploy step completes, SSH into server and run `docker compose ps` — all 4 services should show `healthy` (api, health) or `running` (postgres, nginx).
- [ ] 4.4 Verify `POST http://<host>/api/auth/register` → `POST /api/auth/login` → `GET /api/auth/me` flow works through gateway on port 80
  - **Verification**: From any machine with network access to the server:
    ```bash
    curl -X POST http://<server-ip>/api/auth/register -H "Content-Type: application/json" -d '{"email":"test@example.com","password":"Test123!","displayName":"Test"}'
    # Use the returned token for:
    curl http://<server-ip>/api/auth/me -H "Authorization: Bearer <token>"
    ```
- [ ] 4.5 Verify PostgreSQL port 5432 is NOT exposed on host (`docker compose ps` on server)
  - **Verification**: SSH into server and run `docker compose ps`. The postgres service should show NO port mapping under the `PORTS` column. Alternatively, run `ss -tlnp | grep 5432` — should return nothing.
