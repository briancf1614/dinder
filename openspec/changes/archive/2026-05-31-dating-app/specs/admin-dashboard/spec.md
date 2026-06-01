# Admin Dashboard Specification

## Purpose

Provide authenticated staff tools for user lookup, report review, and moderation actions. All admin endpoints MUST require the `Admin` role.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| AD-1 | User Search by Email or ID | MUST |
| AD-2 | Report Review Queue (filter + resolve) | MUST |
| AD-3 | Ban / Unban User Actions | MUST |
| AD-4 | Append-Only Admin Audit Log | MUST |

### AD-1: User Search by Email or ID

The system MUST allow admins to search users by email (exact or partial match) or user ID (exact match). Results SHALL include: user ID, email, display name, registration date, ban status, and profile-completion flag.

#### Scenario: Search by exact email

- GIVEN an authenticated admin
- WHEN they search for `alice@example.com`
- THEN the matching user record is returned with full account metadata
- AND a summary of recent activity (last login, report count) is included

#### Scenario: Partial email returns multiple matches

- GIVEN users `alice@example.com` and `alice.work@example.com` exist
- WHEN the admin searches for `alice`
- THEN both matching users are returned, paginated (max 50 per page)

### AD-2: Report Review Queue (filter + resolve)

The system MUST present a report review queue sorted by report date, oldest first. Admins SHALL see reporter, reported user, reason, description, and timestamp. Reports MAY be filtered by status (`Pending`, `Resolved`, `Dismissed`).

#### Scenario: Open the pending reports queue

- GIVEN an authenticated admin
- WHEN they navigate to the report review queue
- THEN all `Pending` reports are displayed in chronological order (oldest first)
- AND each row shows reporter, reported user, reason, and report timestamp

#### Scenario: Dismiss a report as no action needed

- GIVEN a report under admin review
- WHEN the admin marks it as `Dismissed` with a note
- THEN the report status changes to `Dismissed`
- AND the audit log records the admin action with timestamp and note

### AD-3: Ban / Unban User Actions

The system MUST allow admins to ban or unban users from the dashboard. Banning SHALL require a mandatory reason and SHALL take immediate effect: revoking sessions, tokens, and SignalR connections. Unbanning SHALL restore normal access.

#### Scenario: Ban from report review

- GIVEN an admin reviewing a confirmed harassment report
- WHEN they select "Ban User" with reason "3 harassment reports confirmed"
- THEN the user is banned immediately (access revoked)
- AND the report is marked `Resolved`
- AND the audit log records the ban

#### Scenario: Unban a user

- GIVEN a previously banned user
- WHEN an admin selects "Unban" with a justification note
- THEN the ban is lifted and the user can log in and access the app normally
- AND the audit log records the unban

### AD-4: Append-Only Admin Audit Log

The system MUST log every admin action — ban, unban, report resolution, photo approval/rejection — with admin ID, action type, target user ID, timestamp, and reason. The audit log SHALL be append-only and immutable.

#### Scenario: Audit entry created on ban

- GIVEN an admin bans user X
- WHEN the ban is executed
- THEN an immutable audit log entry is created with: admin ID, action `BanUser`, target user ID, UTC timestamp, and reason text
