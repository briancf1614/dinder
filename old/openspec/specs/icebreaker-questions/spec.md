# Icebreaker Questions Specification

## Purpose

Auto-assign an icebreaker question when a mutual match is created, removing the pressure of crafting the first message.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| IQ-1 | Auto-Assign Question on Match Creation | MUST |
| IQ-2 | Display in Conversation Header | MUST |
| IQ-3 | Question Library by Category | MUST |
| IQ-4 | Answer Flow with Notification | MAY |

### IQ-1: Auto-Assign on Match

When a mutual match is created, the system MUST assign a random icebreaker question from the library and persist it in the conversation record.

#### Scenario: Match triggers icebreaker
- GIVEN Alice and Bob mutually like each other
- WHEN the `MatchCreated` event fires
- THEN a random icebreaker question is assigned to the conversation
- AND it is stored in the conversation data

### IQ-2: Display in Conversation

The assigned question MUST appear in the conversation header, visible to both matched users.

#### Scenario: Icebreaker visible to both
- GIVEN a match with assigned icebreaker "What's your go-to karaoke song?"
- WHEN either user opens the conversation
- THEN the question is displayed prominently in the conversation header

### IQ-3: Question Library

Admins MUST manage a library of icebreaker questions organized by category (funny, deep, dating, lifestyle). The system SHALL use category weighting when selecting questions for a match.

### IQ-4: Answer Flow

Users MAY answer the icebreaker question within the conversation. When answered, a notification SHALL be sent to the matched user.
