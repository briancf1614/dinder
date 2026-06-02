# AI Photo Moderation Specification

## Purpose

Automate NSFW/violence detection on uploaded photos via Azure AI Vision, reducing manual review load while retaining human override.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| AM-1 | Async AI Scan on Upload Confirm | MUST |
| AM-2 | Auto-Approve Clean Photos | MUST |
| AM-3 | Flagged Photos → Manual Queue with AI Scores | MUST |
| AM-4 | Human Override & User Appeal | MUST |
| AM-5 | Config Toggle for Fallback to Manual-Only | SHOULD |

### AM-1: Async AI Scan on Upload

On photo upload confirmation, the system MUST dispatch an async AI scan via Azure AI Vision (adult/racy/violence detection). Photo status transitions to `AIScanning`.

#### Scenario: Photo scan triggered
- GIVEN a user confirms a photo upload
- WHEN the confirmation event fires
- THEN an async AI scan job is dispatched
- AND photo status is set to `AIScanning`

### AM-2: Auto-Approve Clean

If AI confidence scores are below the configured rejection threshold for all categories, the photo SHALL be auto-approved — skipping the manual queue and becoming publicly visible.

#### Scenario: Clean photo auto-approved
- GIVEN AI scan returns adult=0.01, racy=0.05, violence=0.00 (all below threshold)
- WHEN results are processed
- THEN the photo is auto-approved with no human review

### AM-3: Flagged → Manual Queue

If AI flags any category above threshold, the photo MUST enter the manual moderation queue with status `FlaggedByAI`. AI confidence scores MUST be displayed to the moderator.

#### Scenario: NSFW photo flagged for manual review
- GIVEN AI scan returns adult=0.92 (above threshold)
- WHEN results are processed
- THEN the photo enters the manual queue with status `FlaggedByAI`
- AND the moderator sees "Adult: 92% confidence" alongside the photo

### AM-4: Human Override & Appeal

Moderators MUST be able to override AI decisions (approve a flagged photo, reject an auto-approved one). Users whose photos are rejected SHALL receive a notification and MAY submit an appeal, which re-enters the manual queue.

### AM-5: Config Toggle

If `UseAIModeration` config is `false`, the system SHOULD fall back to the full manual moderation queue with no AI pre-screening.
