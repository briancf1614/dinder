# Profile Prompts Specification

## Purpose

Enable users to add Hinge-style prompts (select from catalog, write ≤150 char answers, up to 3) for richer profiles and conversation starters.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| PP-1 | Prompt Selection & Answer (≤150 chars, max 3) | MUST |
| PP-2 | Prompt Display on Profile & Discovery Cards | MUST |
| PP-3 | Prompt Reordering | SHOULD |
| PP-4 | Admin Prompt Catalog Management | MUST |

### PP-1: Prompt Selection & Answer

Users MUST select prompts from a catalog and provide text answers ≤150 characters. Maximum 3 active prompts per profile.

#### Scenario: Add first prompt
- GIVEN a user editing their profile with 0 prompts
- WHEN they select a catalog prompt and enter an answer ≤150 chars
- THEN the prompt+answer is saved and rendered in the profile preview

#### Scenario: Exceed 3-prompt limit
- GIVEN a user with 3 active prompts
- WHEN they attempt to add a 4th
- THEN the request is rejected with 422

#### Scenario: Answer exceeds 150 chars
- GIVEN a user entering a prompt answer
- WHEN text exceeds 150 characters
- THEN the answer is truncated and a warning is shown

### PP-2: Prompt Display

Selected prompts MUST appear on the user's public profile view and on discovery cards alongside photos and bio.

### PP-3: Prompt Reordering

Users SHOULD be able to reorder their 3 prompts via drag-and-drop or position setter.

### PP-4: Admin Prompt Catalog

Admins MUST manage the catalog: add new prompts, disable/hide prompts, categorize by theme (dating, lifestyle, fun). Disabled prompts SHALL NOT appear in the user-facing catalog but remain on existing profiles.
