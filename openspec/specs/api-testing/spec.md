# API Testing Specification

## Purpose

Specify HTTP integration tests that SHALL run against the Dinder API via `WebApplicationFactory<Program>`. Tests MUST verify endpoint responses without requiring a running server — the factory creates an in-memory test host.

## Requirements

### Requirement: Health Endpoint Returns 200 with JSON

A `GET` request to `/health` MUST return HTTP `200 OK` with a `Content-Type` of `application/json`. The response body SHALL be a JSON object containing `status` (string) and `timestamp` (ISO 8601 string) properties.

#### Scenario: GET /health returns healthy status and timestamp

- GIVEN a test `HttpClient` created from `WebApplicationFactory<Program>`
- WHEN a `GET` request is sent to `/health`
- THEN the response status code MUST be `200`
- AND the response `Content-Type` MUST contain `application/json`
- AND the JSON body MUST contain a `status` property equal to `"healthy"`
- AND the JSON body MUST contain a `timestamp` property parsable as ISO 8601

### Requirement: Root Endpoint Returns 200 with Text

A `GET` request to `/` MUST return HTTP `200 OK` with a text response body (not null or empty).

#### Scenario: GET / returns text content

- GIVEN a test `HttpClient` created from `WebApplicationFactory<Program>`
- WHEN a `GET` request is sent to `/`
- THEN the response status code MUST be `200`
- AND the response body string MUST NOT be null
- AND the response body string MUST NOT be empty
