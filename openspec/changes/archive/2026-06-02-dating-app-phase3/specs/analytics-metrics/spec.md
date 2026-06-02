# Analytics Metrics Specification

## Purpose

Provide aggregate business metrics — growth, conversion, retention, engagement — for the admin dashboard via async, non-blocking aggregation.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| AN-1 | DAU / WAU / MAU Tracking | MUST |
| AN-2 | Subscription Conversion Rate | MUST |
| AN-3 | Match Rate & Swipe-to-Match Ratio | MUST |
| AN-4 | Retention Cohorts (D1/D7/D30) | SHOULD |
| AN-5 | Admin-Only Dashboard API | MUST |

### AN-1: DAU / WAU / MAU

The system MUST track daily, weekly, and monthly active users as distinct user counts computed from login events.

#### Scenario: Query DAU
- GIVEN 150 users logged in today
- WHEN admin queries DAU metric
- THEN the count of distinct user IDs active in the last 24h is returned

### AN-2: Subscription Conversion

The system MUST calculate subscription conversion rate as `subscribed_users / total_registered_users`, filterable by time period.

### AN-3: Match & Swipe Metrics

The system MUST track match rate (matches / total users) and swipe-to-match ratio (matches / total swipes).

#### Scenario: Swipe-to-match ratio
- GIVEN yesterday: 10,000 swipes and 300 matches
- WHEN admin queries the ratio
- THEN 3.0% is returned

### AN-4: Retention Cohorts

The system SHOULD track D1, D7, and D30 retention by signup cohort: what percentage of users who signed up on date X returned on day N.

### AN-5: Admin Dashboard API

All metrics MUST be exposed via admin-only API endpoints. Metric computation SHALL use fire-and-forget MediatR handlers — never blocking the write path. Time filters SHALL support last 7d, 30d, 90d.
