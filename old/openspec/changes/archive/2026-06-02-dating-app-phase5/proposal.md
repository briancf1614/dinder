# Proposal: Dating App Phase 5 — Kotlin Mobile App

## Intent

Ship native Android app (Kotlin 2.2.20 + Jetpack Compose + Material 3) consuming existing backend with ZERO backend changes. Opens Google Play Store channel. Toolchain verified installed — contrary to Phase 4 audit.

## Scope

### In Scope
- Set ANDROID_HOME, JAVA_HOME env vars
- Scaffold `src/Dinder.Mobile/` Compose project (MVVM + Clean Architecture, Hilt DI)
- Ktor HTTP client + JWT management (acquire, refresh, secure storage)
- SignalR WebSocket client for real-time chat + notifications
- Auth screens: login, register, token persistence
- Discovery: swipe card stack, candidate display, match dialog
- Chat: real-time messaging, conversation list, message history, typing indicators
- Notifications: FCM push registration, badge, in-app center
- Update `openspec/config.yaml` mobile toolchain status

### Out of Scope
- iOS, Video Chat (→ Phase 6), Speed Dating (→ deferred), Social Auth, Voice Messages, offline mode, IAP

## Capabilities

### New Capabilities
- `mobile-identity`: Native Android auth (login/register), JWT lifecycle, session via Keystore. Consumes identity-access API.
- `mobile-discovery`: Native swipe UI (card stack, drag gestures), candidate display, match dialog. Consumes discovery API.
- `mobile-chat`: Native real-time chat via SignalR WebSocket, conversation list, read receipts. Consumes real-time-chat API.
- `mobile-notifications`: Native FCM push, notification badge, in-app center. Consumes notifications API.

### Modified Capabilities
None — purely additive mobile client.

## Approach

3 chained PRs (≤400 lines each):

| PR | Scope | Est. Lines | Risk |
|----|-------|-----------|------|
| PR1 | Toolchain + Scaffold + API client | ~200 | Low |
| PR2 | Auth + Discovery screens | ~350 | Medium |
| PR3 | Chat + Notifications | ~350 | Medium |

## Affected Areas

| Area | Impact |
|------|--------|
| `src/Dinder.Mobile/` | New — Android project |
| `openspec/config.yaml` | Modified — mobile status update |
| Backend (all) | None — API consumed as-is |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Emulator performance on Windows | Medium | Physical device fallback |
| SignalR Ktor WebSocket edge cases | Low | JWT query string auth already supported; mirror Angular client reconnect pattern |
| Scope creep beyond 3 PRs | Medium | Strict boundaries; defer extras to Phase 6 |

## Rollback Plan

Angular PWA remains the primary mobile web experience. No backend changes → no DB migration, no API rollback. Remove `src/Dinder.Mobile/`, revert config.yaml. Zero downtime.

## Dependencies

- `ANDROID_HOME` → `%LOCALAPPDATA%\Android\Sdk`
- `JAVA_HOME` → `C:\Program Files\Android\Android Studio\jbr`
- Android emulator or physical device

## Success Criteria

- [ ] `.\gradlew assembleDebug` builds with 0 errors
- [ ] Login with existing credentials → JWT stored securely
- [ ] Swipe UI loads from `GET /api/discovery/candidates`, records via `POST /api/discovery/swipe`
- [ ] Real-time messages delivered via SignalR WebSocket (JWT auth)
- [ ] FCM push received on match event
- [ ] All 223 backend tests still pass
