# Notifications Specification

## Purpose

Deliver push notifications via FCM/APNs, maintain an in-app notification center, manage device tokens, and support per-type notification opt-out.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| NF-1 | Push Notification Dispatch (FCM + APNs) | MUST |
| NF-2 | In-App Notification Center | MUST |
| NF-3 | Device Token Registration | MUST |
| NF-4 | Per-Type Notification Opt-Out | MUST |
| NF-5 | Domain Event → Notification Translation | MUST |

### NF-1: Push Notification Dispatch (FCM + APNs)

The system MUST dispatch push notifications via Firebase Cloud Messaging (FCM) for Android and Apple Push Notification service (APNs) for iOS. Each notification SHALL include a title, body, and deep-link payload. Delivery failures SHALL be logged and the token flagged if permanently invalid (e.g., `NotRegistered`).

#### Scenario: Push delivered successfully

- GIVEN a user with a valid, registered FCM device token
- WHEN a match event occurs for that user
- THEN a push notification is sent with title "New Match!" and body identifying the match
- AND the delivery is logged as successful

#### Scenario: Expired token removed

- GIVEN a device token that FCM returns `NotRegistered` for
- WHEN a notification dispatch attempt fails
- THEN the token is marked as expired and excluded from future dispatches
- AND the notification is still available in the in-app center

### NF-2: In-App Notification Center

The system MUST provide an in-app notification center (bell icon + unread badge). Notifications SHALL be cursor-paginated in reverse chronological order. Users MAY mark notifications as read individually or in bulk.

#### Scenario: View and clear unread notifications

- GIVEN a user with 3 unread notifications (2 match, 1 message)
- WHEN they open the notification center
- THEN all 3 are displayed newest-first with the badge showing 3
- AND tapping "Mark all read" sets all to `IsRead = true` and resets the badge

### NF-3: Device Token Registration

The system MUST allow authenticated users to register FCM or APNs device tokens. Each token SHALL be associated with the current user. If a token is already registered to a different user, it SHALL be reassigned to the current user (device handover or re-login).

#### Scenario: Register a new device token

- GIVEN an authenticated user on a new device
- WHEN they submit their FCM token via the API
- THEN the token is stored and associated with the user
- AND the user becomes eligible for push notifications

### NF-4: Per-Type Notification Opt-Out

The system MUST allow users to opt out of specific notification types: matches, messages, and promotions. Opt-out preferences SHALL be checked before dispatch — opted-out types SHALL still appear in the in-app center but NOT trigger push.

#### Scenario: User disables match notifications

- GIVEN a user opts out of match push notifications
- WHEN a match event occurs
- THEN no push notification is dispatched
- AND the match notification appears in the in-app center only

### NF-5: Domain Event → Notification Translation

The system MUST subscribe to `MatchCreated` (Discovery) and `MessageSent` (Communication) domain events via MediatR. Each event handler SHALL create notification records and dispatch pushes asynchronously, without blocking the source transaction.

#### Scenario: Match event creates notifications for both users

- GIVEN a mutual match between Alice and Bob
- WHEN the `MatchCreated` event is published
- THEN notification records are created for both Alice and Bob
- AND push notifications are dispatched for each user if they have registered devices
