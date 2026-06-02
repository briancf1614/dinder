# Delta for Real-Time Chat

## ADDED Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| RC-6 | Conversation List Query | MUST |

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
