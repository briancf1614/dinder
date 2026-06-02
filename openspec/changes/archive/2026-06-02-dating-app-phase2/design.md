# Design: Dating App Phase 2 — Monetization

## Technical Approach

New `subscription` bounded context following existing per-schema PostgreSQL pattern (as done for `discovery.*`, `notification.*`). Stripe Checkout Session API for payment UI — zero raw card data. Entitlement enforcement via MediatR `IPipelineBehavior` reading tier from JWT claims (no DB round-trip at gate). Angular PWA manifest + service worker for mobile browser installability.

## Architecture Decisions

| Decision | Choice | Alternatives Rejected | Rationale |
|----------|--------|----------------------|-----------|
| **Subscription context location** | Folders under existing `Dinder.Domain`/`Application`/`Infrastructure`/`Api` projects | New `Dinder.Subscription` project | Modular monolith convention already established (8 contexts in same projects). Future extraction still possible via folder moves |
| **Entitlement enforcement point** | MediatR `IPipelineBehavior<,>` (`EntitlementBehavior`) | ASP.NET middleware, Action filter attribute | Middleware can't see `[RequiresTier]` on commands. Action filter only covers HTTP. Pipeline behavior is the single choke point for HTTP, SignalR, and background jobs |
| **Tier claim in JWT** | Add `tier` claim at issuance; 15-min expiry is natural revocation window | Separate entitlement service with Redis cache | 15-min TTL is acceptable staleness (EE-4). No extra infra. Token refresh on subscription change reissues with new tier |
| **Stripe webhook auth** | `Stripe-Signature` header verification + raw body buffering | API key in header, IP whitelist | Stripe SDK built-in `EventUtility.ConstructEvent`. No custom auth scheme |
| **Idempotency for webhooks** | Upsert by `StripeSubscriptionId` + version check on `StripeEvent.Created` timestamp | Deduplication table | Simpler: subscription record is the dedup source. Replayed events with older timestamps are no-ops |
| **Premium features exposure** | Checkout session `metadata[user_id]` for linking; webhook updates User aggregate directly | Separate customer mapping service | User already in identity schema; one less indirection. `StripeCustomerId` on User for portal linking |

## Data Flow

```
User clicks Upgrade ──► POST /subscription/checkout ──► StripeService.CreateSession()
       │                                                        │
       │                                              Stripe Checkout (external)
       │                                                        │
       ▼                                              checkout.session.completed
  Cancel URL ◄─────────────────────────────────────────── (webhook) ──┐
                                                                       │
  Success URL ◄────────────────────────────────────────── Stripe redirect ──┐
                                                                       │
                          POST /webhooks/stripe ◄───────────────────────────┘
                                  │
                    EventUtility.ConstructEvent()  (Stripe-Signature verify)
                                  │
                    Idempotency: upsert Subscription, set User.Tier
                                  │
                    User.Tier updated → next JWT refresh carries new tier
                                  │
                    MediatR pipeline: [RequiresTier(Plus)] → reads tier from JWT
```

## File Changes

| Path | Action | Description |
|------|--------|-------------|
| `src/Dinder.Domain/Entities/Subscription.cs` | Create | Subscription aggregate (Id, UserId, StripeSubscriptionId, Tier, Status, CurrentPeriodEnd) |
| `src/Dinder.Domain/Enums/SubscriptionTier.cs` | Create | Free, Plus, Premium enum |
| `src/Dinder.Domain/Enums/SubscriptionStatus.cs` | Create | Active, PastDue, Canceled, Expired enum |
| `src/Dinder.Domain/Entities/User.cs` | Modify | Add `SubscriptionTier Tier` + `string? StripeCustomerId` |
| `src/Dinder.Application/Subscription/Commands/CreateCheckoutSessionCommand.cs` | Create | CQRS: validate current tier, call StripeService, return session URL |
| `src/Dinder.Application/Subscription/Commands/CreatePortalSessionCommand.cs` | Create | Return Stripe Customer Portal URL for managing billing |
| `src/Dinder.Application/Subscription/Queries/GetSubscriptionStatusQuery.cs` | Create | Return current tier, status, renewal date |
| `src/Dinder.Application/Common/Behaviors/EntitlementBehavior.cs` | Create | MediatR pipeline: read `[RequiresTier]` attribute, check JWT `tier` claim, reject 403 |
| `src/Dinder.Application/Common/Attributes/RequiresTierAttribute.cs` | Create | `[RequiresTier(SubscriptionTier.Plus)]` — marker on IRequest types |
| `src/Dinder.Application/Common/Interfaces/IStripeService.cs` | Create | `CreateCheckoutSession`, `CreatePortalSession`, `ConstructWebhookEvent` |
| `src/Dinder.Application/Identity/Commands/RefreshTokenCommand.cs` | Modify | Include tier claim via `IJwtService.GenerateAccessToken(userId, email, tier)` |
| `src/Dinder.Application/Identity/Commands/LoginCommand.cs` | Modify | Same — pass User.Tier to JWT generation |
| `src/Dinder.Application/Discovery/Commands/SwipeCommand.cs` | Modify | Tier-aware limit: 25/100/unlimited. Return `upgrade_url` on 429 |
| `src/Dinder.Application/Discovery/Commands/UndoSwipeCommand.cs` | Create | `[RequiresTier(Plus)]`, removes last swipe and decrements counter |
| `src/Dinder.Infrastructure/Payments/StripeService.cs` | Create | Stripe.Checkout.Session, Stripe.BillingPortal.Session, EventUtility |
| `src/Dinder.Infrastructure/Payments/StripeConfiguration.cs` | Create | Read `Stripe:SecretKey`, `Stripe:WebhookSecret`, price IDs from config |
| `src/Dinder.Infrastructure/Persistence/SubscriptionDbContext.cs` | Create | New context with `subscription.*` schema |
| `src/Dinder.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs` | Create | EF Core config for Subscription entity |
| `src/Dinder.Infrastructure/Auth/JwtService.cs` | Modify | `GenerateAccessToken` gains `string tier` param → adds `tier` claim |
| `src/Dinder.Api/Controllers/SubscriptionController.cs` | Create | `POST checkout`, `GET status`, `POST portal` |
| `src/Dinder.Api/Controllers/WebhookController.cs` | Create | `POST stripe` — unauthenticated, raw body for signature |
| `src/Dinder.Api/Program.cs` | Modify | Register `EntitlementBehavior`, `SubscriptionDbContext`, Stripe config. Enable raw body buffering for webhook route |
| `docker-compose.yml` | Modify | Add `stripe-cli` service with `stripe listen --forward-to` |
| `src/app/manifest.json` | Create | Angular PWA manifest |
| `src/app/ngsw-config.json` | Create | Service worker config |

## Interfaces / Contracts

### REST Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/v1/subscription/checkout` | Required | Body: `{tier}` → returns `{sessionUrl}` |
| `GET` | `/api/v1/subscription/status` | Required | Returns `{tier, status, currentPeriodEnd, customerPortalUrl}` |
| `POST` | `/api/v1/subscription/portal` | Required | Returns `{portalUrl}` for managing billing |
| `POST` | `/api/v1/webhooks/stripe` | Stripe-Signature | Raw body, verified by `EventUtility.ConstructEvent` |
| `POST` | `/api/v1/discovery/undo` | Required | `[RequiresTier(Plus)]` — undoes last swipe |
| `GET` | `/api/v1/discovery/likes` | Required | `[RequiresTier(Plus)]` — who liked you |
| `POST` | `/api/v1/discovery/boost` | Required | `[RequiresTier(Premium)]` — profile boost (1/month) |
| `POST` | `/api/v1/discovery/swipe` | Modified | 429 response now includes `upgrade_url` |

### MediatR Attribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class RequiresTierAttribute : Attribute
{
    public SubscriptionTier MinimumTier { get; }
    public RequiresTierAttribute(SubscriptionTier minimumTier) => MinimumTier = minimumTier;
}
```

### JWT claim change

```diff
- { sub, email, jti }
+ { sub, email, tier, jti }
```

### Stripe Configuration (appsettings)

```json
{
  "Stripe": {
    "SecretKey": "",
    "WebhookSecret": "",
    "Prices": {
      "Plus": "price_xxx",
      "Premium": "price_yyy"
    }
  }
}
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | EntitlementBehavior tier gating, SwipeCommand tier-aware limit, SubscriptionStatus progression | xUnit with mocked IStripeService, IHttpContextAccessor |
| Integration | SubscriptionDbContext writes, Stripe webhook idempotency, JWT tier claim round-trip | Testcontainers (PostgreSQL); Stripe CLI `stripe trigger` |
| Contract | New REST endpoints | Swashbuckle OpenAPI validation |

## Migration / Rollout

- `dotnet ef migrations add AddSubscriptionTier` on DinderDbContext (identity schema)
- `dotnet ef migrations add InitialSubscription` on new SubscriptionDbContext
- All existing users default to `Free` tier — migration sets default value
- Rollback: drop `subscription` schema, drop `Tier` and `StripeCustomerId` columns from `identity.users`
- Feature flag `EnableSubscriptions` in config to disable monetization without code rollback

## Open Questions

- [ ] Stripe test API keys provisioned? Which Stripe account?
- [ ] Plus/Premium price IDs finalized? ($9.99 / $19.99 assumed)
- [ ] Success/cancel URLs for Checkout — custom on SPA or generic API redirect?
