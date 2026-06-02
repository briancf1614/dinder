# Real-Time Chat Specification

## Purpose

Enable matched users to exchange text messages in real time via SignalR. Chat is strictly restricted to mutual matches and supports read receipts and unmatch.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| RC-1 | SignalR Real-Time Message Exchange | MUST |
| RC-2 | Per-Message Read Receipts | MUST |
| RC-3 | Unmatch Action (hide + retain) | MUST |
| RC-4 | Match-Gated Access Control | MUST |
| RC-5 | Cursor-Paginated Message History | MUST |
| RC-6 | Conversation List Query | MUST |

### RC-1: SignalR Real-Time Message Exchange

The system MUST deliver text messages in real time between matched users via SignalR WebSocket connections. Messages MUST be persisted before delivery acknowledgment. Content SHALL be limited to 2000 characters.

#### Scenario: Real-time delivery to online match

- GIVEN Alice and Bob (matched) are both connected via SignalR
- WHEN Alice sends a "Hello!" message
- THEN the message is persisted to `communication.messages`
- AND it is delivered to Bob in real time via the SignalR hub
- AND Alice receives a delivery acknowledgment

#### Scenario: Recipient offline

- GIVEN Bob is not connected via SignalR
- WHEN Alice sends a message to Bob
- THEN the message is persisted
- AND Bob receives it on next connection via message history retrieval

### RC-2: Per-Message Read Receipts

The system MUST mark messages as read when the recipient views the conversation. Read receipts SHALL be per-message with individual `ReadAt` timestamps. The sender SHALL receive a read receipt via SignalR.

#### Scenario: Messages marked read on conversation view

- GIVEN Alice sent 3 unread messages to Bob
- WHEN Bob opens the conversation with Alice
- THEN all 3 messages receive `ReadAt` timestamps
- AND Alice receives a read receipt notification via SignalR for the most recent message

### RC-3: Unmatch Action (hide + retain)

The system MUST allow either user to unmatch at any time. Unmatching SHALL hide the conversation from both users' UI. Messages MUST be retained in the database for moderation but not accessible to either user post-unmatch.

#### Scenario: User unmatches a match

- GIVEN Alice and Bob have an active conversation
- WHEN Alice selects "Unmatch"
- THEN the match status is set to `Unmatched`
- AND the conversation is removed from both users' conversation lists
- AND neither user can send new messages to the other
- AND existing messages are retained in the database (not deleted)

### RC-4: Match-Gated Access Control

The system MUST restrict conversation access to the two matched participants only. Conversations SHALL be created automatically upon mutual match. Non-participants and users who have unmatched MUST be denied access.

#### Scenario: Third-party access denied

- GIVEN a conversation between Alice and Bob
- WHEN Charlie (any other user) attempts to access the conversation via the API
- THEN the request is rejected with 403 Forbidden

### RC-5: Cursor-Paginated Message History

The system MUST provide cursor-based paginated message history ordered by timestamp ascending. Page size SHALL default to 50 messages.

#### Scenario: Load first page of history

- GIVEN a conversation with 120 messages
- WHEN the user requests message history
- THEN the 50 most recent messages are returned with a cursor for the next page

#### Scenario: Empty conversation (new match, no messages yet)

- GIVEN a newly created conversation with zero messages
- WHEN the user requests message history
- THEN an empty list is returned with no next-page cursor

### RC-6: Conversation List Query

The system MUST provide a `GET /api/v1/conversations` endpoint returning a paginated list of the authenticated user's active (non-unmatched) conversations. Each entry SHALL include the match's display name, last message preview, unread message count, and icebreaker data. Results SHALL be ordered by most recent message descending. Pagination SHALL use cursor-based keys with a default page size of 20.

#### Scenario: Retrieve conversations with last message

- GIVEN Alice has 3 active conversations — 1 with unread messages, 2 without
- WHEN Alice requests `GET /api/v1/conversations`
- THEN 3 conversation entries are returned ordered by most recent message
- AND each entry includes the match's name, last message preview, and unread count
- AND a pagination cursor is included if more than 20 entries exist

#### Scenario: Unmatched conversations excluded

- GIVEN Alice has previously unmatched Bob
- WHEN Alice requests `GET /api/v1/conversations`
- THEN Bob's conversation does NOT appear in the response

#### Scenario: Icebreaker data included

- GIVEN Alice and Bob matched via an icebreaker "What's your favorite travel destination?"
- WHEN Alice requests `GET /api/v1/conversations`
- THEN Bob's entry includes the icebreaker question text

#### Scenario: Empty conversation list

- GIVEN a new user with no matches or conversations
- WHEN they request `GET /api/v1/conversations`
- THEN an empty list is returned with no pagination cursor
