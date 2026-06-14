# Delta for Admin Dashboard

## ADDED Requirements

### Requirement: AD-5 Analytics Widgets

The admin dashboard MUST display analytics widgets: DAU/WAU/MAU charts, subscription conversion rate, match rate, swipe-to-match ratio. Time filters SHALL support last 7d, 30d, 90d. All data MUST be computed via async aggregation (fire-and-forget).

#### Scenario: View growth metrics
- GIVEN an authenticated admin on the dashboard
- WHEN the analytics tab is selected with "last 30 days" filter
- THEN DAU/WAU/MAU charts render with daily data points
- AND subscription conversion percentage is displayed

### Requirement: AD-6 AI Moderation Queue View

The moderation queue MUST display AI confidence scores for flagged photos. Filters SHALL support AI decision type: auto-approved, flagged, appealed.

#### Scenario: Filter by AI-flagged photos
- GIVEN the moderation queue
- WHEN the admin filters by "FlaggedByAI"
- THEN only photos flagged by AI are displayed with their confidence scores

## MODIFIED Requirements

### Requirement: AD-2: Report Review Queue (filter + resolve)

The system MUST present a report review queue sorted by report date, oldest first. Each row SHALL include reporter, reported user, reason, sub-category, description, and timestamp. Reports MAY be filtered by status and sub-category.
(Previously: No sub-category column or filter)

#### Scenario: Open the pending reports queue
- GIVEN an authenticated admin
- WHEN they navigate to the report review queue
- THEN all `Pending` reports display with reason, sub-category, and timestamp

#### Scenario: Filter by sub-category
- GIVEN the reports queue
- WHEN the admin filters by "Verbal Abuse"
- THEN only matching reports are shown

#### Scenario: Dismiss a report as no action needed
- GIVEN a report under review
- WHEN the admin marks it `Dismissed` with a note
- THEN status changes and the audit log records the action
