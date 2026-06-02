# Subscription Management Specification

## Purpose

Manage subscription tiers (Free, Plus, Premium), Stripe Checkout payment flow, and the full subscription lifecycle via Stripe webhooks. Users never touch raw card data — all payment UI is delegated to Stripe.

## Requirements

| ID | Requirement | Strength |
|----|-------------|----------|
| SM-1 | Tier Model | MUST |
| SM-2 | Stripe Checkout Initiation | MUST |
| SM-3 | Webhook Lifecycle Syncing | MUST |
| SM-4 | Subscription Status Progression | MUST |

### SM-1: Tier Model

The system MUST define three tiers: **Free** (default, $0), **Plus** ($9.99/mo), and **Premium** ($19.99/mo). Each tier SHALL have a Stripe Price ID. The User aggregate's `Tier` property MUST default to `Free` on registration.

#### Scenario: New user defaults to Free tier

- GIVEN a newly registered user
- WHEN their account is created
- THEN their `Tier` is `Free`
- AND they have no active Stripe subscription

### SM-2: Stripe Checkout Initiation

The system MUST create a Stripe Checkout Session for the requested tier and return the session URL. Sessions SHALL be stateless — Stripe handles the entire payment UI. On success, Stripe redirects to a configurable success URL; on cancel, to a cancel URL.

#### Scenario: User initiates Plus subscription

- GIVEN an authenticated Free-tier user
- WHEN they request to upgrade to Plus
- THEN a Stripe Checkout Session is created with the Plus Price ID
- AND the session URL is returned
- AND no card data enters the system

#### Scenario: Already subscribed user attempts duplicate

- GIVEN a user with an active Plus subscription
- WHEN they request to upgrade to Plus again
- THEN the request is rejected with 409 Conflict

### SM-3: Webhook Lifecycle Syncing

The system MUST handle three Stripe webhook events via a dedicated unauthenticated endpoint (verified by Stripe signature): `checkout.session.completed`, `customer.subscription.updated`, and `customer.subscription.deleted`. Every event MUST be idempotent (duplicate events produce no side effects).

#### Scenario: Checkout completed — activation

- GIVEN a completed Stripe Checkout Session for user U on tier T
- WHEN the `checkout.session.completed` webhook arrives
- THEN user U's tier is set to T
- AND subscription status is set to `active`
- AND the event is idempotent (replayed event is a no-op)

#### Scenario: Subscription deleted — downgrade

- GIVEN an active Plus subscriber whose subscription is canceled (non-payment)
- WHEN `customer.subscription.deleted` arrives
- THEN tier reverts to `Free`
- AND subscription status is set to `canceled`

### SM-4: Subscription Status Progression

The system MUST track four statuses: `active`, `past_due`, `canceled`, `expired`. Past-due subscriptions SHALL retain entitlements for a 7-day grace period before expiration. Expired subscriptions MUST revert to Free tier.

#### Scenario: Payment failure — past_due

- GIVEN a Plus subscriber with a failed renewal payment
- WHEN `customer.subscription.updated` arrives with status `past_due`
- THEN subscription status transitions to `past_due`
- AND entitlements remain for 7 days

#### Scenario: Grace period exhausted — expired

- GIVEN a subscription in `past_due` for 8 days
- WHEN the system evaluates the grace period
- THEN tier reverts to `Free`
- AND status transitions to `expired`
