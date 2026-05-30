# Media Storage Specification

## Purpose

Provide secure, CDN-accelerated file upload and delivery for user photos. Use pre-signed URLs for direct client-to-blob uploads, avoiding API server bandwidth consumption.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| MS-1 | Pre-Signed Upload URL Generation | MUST |
| MS-2 | Upload Confirmation & Moderation Trigger | MUST |
| MS-3 | CDN Delivery for Approved Media | MUST |
| MS-4 | Media Deletion (GDPR cascade) | MUST |

### MS-1: Pre-Signed Upload URL Generation

The system MUST generate time-limited pre-signed URLs (Azure Blob SAS or S3 pre-signed) for direct client-to-storage uploads. URLs SHALL expire after 10 minutes. Allowed content types MUST be restricted to `image/jpeg`, `image/png`, and `image/webp`. Maximum file size SHALL be 10 MB.

#### Scenario: Request upload URL for profile photo

- GIVEN an authenticated user with fewer than 6 photos
- WHEN they request an upload URL specifying `image/jpeg`
- THEN a pre-signed PUT URL is returned with a 10-minute expiry
- AND the blob key is scoped to `users/{userId}/photos/{guid}.jpg`

#### Scenario: Expired upload URL rejected by storage

- GIVEN a pre-signed URL generated 11 minutes ago
- WHEN the client attempts to upload to the expired URL
- THEN the storage provider rejects the upload with 403 Forbidden

### MS-2: Upload Confirmation & Moderation Trigger

The system MUST expose a confirmation endpoint accepting the blob key. On confirmation it SHALL verify blob existence, persist a `MediaFile` record with status `PendingReview`, and trigger the photo moderation pipeline.

#### Scenario: Confirm successful upload

- GIVEN a client uploaded a photo to the pre-signed URL
- WHEN they call the confirmation endpoint with the blob key
- THEN blob existence is verified via storage SDK
- AND a `MediaFile` record is created with status `PendingReview`
- AND the photo moderation pipeline is triggered

#### Scenario: Confirm non-existent blob

- GIVEN a client submits a blob key for an upload that never completed
- WHEN the confirmation endpoint is called
- THEN blob-existence verification fails
- AND the endpoint returns 404 Not Found

### MS-3: CDN Delivery for Approved Media

The system MUST serve approved photos through a CDN (Azure CDN or CloudFront). CDN URLs SHALL be returned to clients instead of direct blob storage URLs. Cache headers SHALL be set for optimal browser caching.

#### Scenario: Retrieve approved photo URL

- GIVEN a photo with status `Approved`
- WHEN a client requests the photo URL
- THEN a CDN URL is returned (e.g., `https://cdn.dinder.com/photos/{key}`)
- AND the CDN serves the photo with `Cache-Control: public, max-age=86400`

### MS-4: Media Deletion (GDPR cascade)

The system MUST delete media files from storage and metadata from the database as part of GDPR account deletion. A soft-delete with up to 30-day retention MAY precede hard deletion.

#### Scenario: Cascading delete on account erasure

- GIVEN an account deletion is in progress
- WHEN the GDPR cascade reaches the media context
- THEN all blobs owned by the user are deleted from blob storage
- AND all `MediaFile` records for the user are removed from the database
