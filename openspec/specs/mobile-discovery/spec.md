# Mobile Discovery Specification

## Purpose

Native swipe-based candidate browsing (Tinder-style card stack) consuming the existing discovery API. Handles candidate display, swipe recording, match animation, and swipe limit awareness.

## Requirements

| ID | Requirement | API |
|----|-------------|-----|
| MD-1 | Swipe Card Stack UI | — |
| MD-2 | Candidate Loading | GET /discovery/candidates |
| MD-3 | Swipe Action Recording | POST /discovery/swipe |
| MD-4 | Mutual Match Animation | — |
| MD-5 | Daily Swipe Limit Display | (429 response) |

### MD-1: Swipe Card Stack UI

The app MUST render a stack of candidate cards using Jetpack Compose drag gestures. Each card SHALL show the candidate's primary photo, display name, age, and first prompt. Right swipe (drag right > threshold) triggers a like; left swipe triggers a pass. Cards beyond the top two MAY be pre-loaded lazily. An empty stack SHALL show a "no more candidates" placeholder.

#### Scenario: Swipe right on candidate

- GIVEN a stack with candidate Alice on top
- WHEN user drags Alice right past the swipe threshold
- THEN the card animates off-screen right with a like indicator
- AND a POST /discovery/swipe {direction: Right} is fired

#### Scenario: Empty stack — tap to refresh

- GIVEN zero candidates returned from API
- WHEN user sees the "No more candidates" placeholder
- THEN a "Refresh" button is visible that re-fetches candidates

### MD-2: Candidate Loading

The app MUST fetch candidates via `GET /discovery/candidates` with optional lat/lng from device GPS. Results SHALL populate the card stack in order. Pull-to-refresh SHALL re-fetch. Network errors SHALL show a retry snackbar without clearing the existing stack.

#### Scenario: Successful load with location

- GIVEN location permission granted and GPS coordinates available
- WHEN the discovery screen loads
- THEN candidates are fetched with lat/lng and displayed in the card stack

### MD-3: Swipe Action Recording

Each swipe MUST POST to `/discovery/swipe` with `{swipedId, direction}`. The swipe SHALL be sent optimistically — UI removes the card immediately. On 429 (limit reached), the card SHALL return to stack and the limit warning SHALL display with reset time.

#### Scenario: Swipe limit reached

- GIVEN user has exhausted daily swipes
- WHEN they swipe on a candidate
- THEN the card returns to the stack
- AND a banner shows "Daily limit reached — resets at {resetAt}"

### MD-4: Mutual Match Animation

When POST /discovery/swipe returns a match response, the app MUST display a full-screen match animation (overlay with both users' photos + "It's a Match!"). User dismisses to continue swiping.

#### Scenario: Mutual match detected

- GIVEN user swipes right on a candidate who previously liked them
- WHEN the API response includes a match
- THEN a full-screen "It's a Match!" overlay appears with both photos
- AND tapping "Continue" dismisses overlay and returns to stack

### MD-5: Daily Swipe Limit Display

The app SHALL query remaining swipes from the API response (429 body) or a separate endpoint. A progress indicator SHALL show remaining swipes vs. daily limit. The display MUST update after each swipe.

#### Scenario: Remaining swipes shown

- GIVEN user has 20 of 50 swipes remaining
- WHEN viewing the discovery screen
- THEN a chip shows "20 left today" or a progress bar at 60% used
