# Admin Dashboard Specification

## Purpose

Provide authenticated staff tools for user lookup, report review, moderation actions, analytics, and AI-pre-screened photo queues. All admin endpoints MUST require the `Admin` role.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| AD-1 | User Search by Email or ID | MUST |
| AD-2 | Report Review Queue (filter by status + sub-category, resolve) | MUST |
| AD-3 | Ban / Unban User Actions | MUST |
| AD-4 | Append-Only Admin Audit Log | MUST |
| AD-5 | Analytics Widgets (DAU, conversion, matches) | MUST |
| AD-6 | AI Moderation Queue View | MUST |

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

The system MUST present a report review queue sorted by report date, oldest first. Each row SHALL include reporter, reported user, reason, sub-category, description, and timestamp. Reports MAY be filtered by status (`Pending`, `Resolved`, `Dismissed`) and sub-category.

#### Scenario: Open the pending reports queue

- GIVEN an authenticated admin
- WHEN they navigate to the report review queue
- THEN all `Pending` reports display with reason, sub-category, and timestamp

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

### AD-5: Analytics Widgets

The admin dashboard MUST display analytics widgets: DAU/WAU/MAU charts, subscription conversion rate, match rate, swipe-to-match ratio. Time filters SHALL support last 7d, 30d, 90d. All data MUST be computed via async aggregation (fire-and-forget).

#### Scenario: View growth metrics

- GIVEN an authenticated admin on the dashboard
- WHEN the analytics tab is selected with "last 30 days" filter
- THEN DAU/WAU/MAU charts render with daily data points
- AND subscription conversion percentage is displayed

### AD-6: AI Moderation Queue View

The moderation queue MUST display AI confidence scores for flagged photos. Filters SHALL support AI decision type: auto-approved, flagged, appealed.

#### Scenario: Filter by AI-flagged photos

- GIVEN the moderation queue
- WHEN the admin filters by "FlaggedByAI"
- THEN only photos flagged by AI are displayed with their confidence scores
