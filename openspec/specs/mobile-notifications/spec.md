# Mobile Notifications Specification

## Purpose

Native push notifications via FCM plus real-time in-app notification delivery via NotificationHub SignalR. Handles device token registration, notification list, badge count, and per-type opt-out.

## Requirements

| ID | Requirement | API |
|----|-------------|-----|
| MN-1 | FCM Push Registration | POST /notifications/register-token |
| MN-2 | Notification List (paginated) | GET /notifications |
| MN-3 | Real-Time Delivery via SignalR | NotificationHub WebSocket |
| MN-4 | Badge Count | (SignalR BadgeUpdate) |
| MN-5 | Per-Type Opt-Out | PUT /notifications/opt-out |
| MN-6 | Mark as Read | POST /notifications/read |

### MN-1: FCM Push Registration

The app MUST obtain an FCM token from Firebase SDK and register it via `POST /notifications/register-token` with `{token, platform: Android}`. Registration SHALL happen after login and on token refresh. If the API returns an error, registration SHALL be retried on next app launch.

#### Scenario: Token registered after login

- GIVEN user just logged in and FCM SDK returned a token
- WHEN the app sends POST /notifications/register-token
- THEN the server responds 204 and push notifications are enabled

### MN-2: Notification List

The app MUST fetch notifications via `GET /notifications?cursor=&limit=20` with infinite scroll. Each entry SHALL display type icon (match/message), title, body, and relative timestamp. Tapping a notification SHALL deep-link to the relevant screen (conversation or match).

#### Scenario: Notification deep-links to conversation

- GIVEN user has a "New message from Alice" notification
- WHEN they tap the notification
- THEN the app navigates to the chat screen with Alice's conversation open

### MN-3: Real-Time Delivery via SignalR

The app MUST connect to NotificationHub at `/hubs/notifications` with JWT auth. On receiving `NewNotification`, the notification SHALL be inserted at the top of the in-app list. On receiving `BadgeUpdate`, the badge count SHALL update. Connection SHALL auto-reconnect on disconnect with the same backoff strategy as ChatHub.

#### Scenario: Real-time notification received

- GIVEN user is on any screen with NotificationHub connected
- WHEN server pushes a NewNotification via SignalR
- THEN the notification appears at top of the in-app list
- AND the badge count increments

### MN-4: Badge Count

The app badge (app icon + in-app bell icon) SHALL reflect the server-pushed `BadgeUpdate.unreadCount`. On cold start, the count SHALL be derived from the first page of `GET /notifications` (count of `isRead: false` entries). Tapping "Mark all read" SHALL reset to zero.

### MN-5: Per-Type Opt-Out

The app MUST display toggle switches for match, message, and promotional notification types. Toggling SHALL call `PUT /notifications/opt-out {type, optOut}`. The UI SHALL reflect current opt-out state from GET /notifications metadata.

#### Scenario: User disables match push notifications

- GIVEN user toggles "Match notifications" off in settings
- WHEN the opt-out API call succeeds (204)
- THEN match push notifications stop, but match notifications still appear in the in-app list

### MN-6: Mark as Read

Tapping "Mark all read" SHALL call `POST /notifications/read` (no body = mark all). Individual notification tap SHALL call `POST /notifications/read {notificationIds: [id]}`. On success, the UI SHALL update read state and badge count.
