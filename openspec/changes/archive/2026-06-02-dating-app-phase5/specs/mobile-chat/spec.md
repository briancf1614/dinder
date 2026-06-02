# Mobile Chat Specification

## Purpose

Native real-time chat via Ktor WebSocket client (SignalR protocol) consuming the existing ChatHub and Chat REST API. Handles conversations list, live messaging, typing indicators, icebreaker display, and unmatch.

## Requirements

| ID | Requirement | API |
|----|-------------|-----|
| MC-1 | Conversation List | GET /chat/conversations |
| MC-2 | Real-Time Messaging via SignalR | ChatHub WebSocket |
| MC-3 | Message History (paginated) | GET /chat/conversations/{id}/messages |
| MC-4 | Typing Indicator | ChatHub.TypingIndicator |
| MC-5 | Icebreaker Display | — (conversation metadata) |
| MC-6 | Unmatch Action | POST /chat/conversations/{id}/unmatch |
| MC-7 | WebSocket Connection Lifecycle | ChatHub (join/leave/auto-reconnect) |

### MC-1: Conversation List

The app MUST fetch conversations via `GET /chat/conversations` with cursor pagination. Each row SHALL display match name, last message preview, unread count badge, and match avatar. The list SHALL support pull-to-refresh and infinite scroll (cursor-based).

#### Scenario: Conversations loaded with unread counts

- GIVEN user has 3 active conversations, 1 unread
- WHEN the chat list screen loads
- THEN 3 rows appear ordered by most recent message
- AND the unread conversation shows a count badge

### MC-2: Real-Time Messaging via SignalR

The app MUST connect to ChatHub at `/hubs/chat` with JWT auth via query string. On connection, it SHALL call `JoinConversation` for the active conversation. Messages sent via `SendMessage` MUST be received via server-pushed `ReceiveMessage`. The app SHALL call `MarkRead` when the user views messages.

#### Scenario: Send and receive a message in real time

- GIVEN Alice and Bob are both connected and joined to their conversation
- WHEN Alice sends "Hey!" via SendMessage
- THEN Alice sees her own message appear immediately
- AND Bob receives it via ReceiveMessage in real time

#### Scenario: Message sent while recipient offline

- GIVEN Bob is not connected via SignalR
- WHEN Alice sends a message
- THEN the message is sent successfully and persisted server-side
- AND Bob will see it when he reconnects (via MC-3 history)

### MC-3: Message History

The app MUST fetch paginated history via `GET /chat/conversations/{id}/messages?cursor=&limit=50`. Messages SHALL display in ascending order with load-more at top. Sender's own messages SHALL be right-aligned; match's left-aligned.

### MC-4: Typing Indicator

When the user types, the app MUST call `TypingIndicator(conversationId, true)` with a 3-second debounce (false on idle). On receiving `TypingUpdate`, the UI SHALL show "{name} is typing…" below the match's last message.

### MC-5: Icebreaker Display

When a conversation was created via icebreaker, the icebreaker question text from conversation metadata SHALL display as a banner above the message list.

### MC-6: Unmatch Action

The app MUST show a confirmation dialog before calling `POST /chat/conversations/{id}/unmatch`. On success (204), the conversation SHALL be removed from the list and the chat screen dismissed.

### MC-7: WebSocket Connection Lifecycle

The Ktor WebSocket client SHALL auto-reconnect on disconnect with exponential backoff (mirroring Angular PWA pattern: 1s, 2s, 4s, 8s, max 30s). On foreground (app resume), it SHALL verify connection and re-join active conversation groups. On background, it SHALL call `LeaveConversation` and disconnect gracefully.

#### Scenario: App returns from background

- GIVEN app was backgrounded and WebSocket disconnected
- WHEN app comes to foreground
- THEN WebSocket reconnects with JWT
- AND active conversation group is re-joined
- AND any missed messages are fetched via REST history
