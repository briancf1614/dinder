# CI/CD Deploy Specification

## Purpose

Automated deployment via GitHub Actions SSH pipeline. Push to `main` triggers build and restart on the Oracle ARM64 server.

## Requirements

### Requirement: Push to Main Triggers Deployment

The workflow MUST trigger on push to `main`. It MUST SSH into the server, pull code, write `.env` from GitHub Secrets, build ARM64-native images, and restart services via `docker compose up -d --force-recreate`.

#### Scenario: Successful deploy

- GIVEN a commit pushed to `main`
- WHEN the workflow runs
- THEN all 4 services are healthy within 5 minutes

#### Scenario: SSH connection fails

- GIVEN a commit pushed to `main`
- WHEN SSH to the server fails
- THEN the workflow fails with a clear error
- AND existing services are not disrupted

#### Scenario: Docker build fails

- GIVEN a commit pushed to `main`
- WHEN `docker compose build` fails
- THEN the workflow fails reporting the build error

### Requirement: Secrets Are Never Exposed

Secrets (`SSH_HOST`, `SSH_USER`, `SSH_KEY`, `POSTGRES_PASSWORD`, `JWT_SECRET`) MUST use `${{ secrets.* }}` only. `.env` MUST be written without echoing values to logs.

#### Scenario: Secrets written without exposure

- GIVEN the workflow writes `.env` on the server
- WHEN the file is created via SSH
- THEN no secret values appear in workflow logs

#### Scenario: Missing required secret

- GIVEN a required secret is not configured
- WHEN the workflow runs
- THEN it fails early identifying which secret is missing
