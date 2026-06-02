using Dinder.Application.Common.Behaviors;
using Dinder.Infrastructure.Extensions;
using Dinder.Infrastructure.SignalR;
using FluentValidation;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP context accessor (needed by EntitlementBehavior)
builder.Services.AddHttpContextAccessor();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Dinder.Application.Identity.Commands.RegisterCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(EntitlementBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Dinder.Application.Identity.Commands.RegisterCommand).Assembly);

// SignalR
builder.Services.AddSignalR();

// Feature flags
builder.Services.AddSingleton(new SubscriptionFeatureFlags
{
    EnableSubscriptions = builder.Configuration.GetValue<bool>("Features:EnableSubscriptions"),
});

builder.Services.AddSingleton(new AiModerationFeatureFlags
{
    UseAIModeration = builder.Configuration.GetValue<bool>("Azure:UseAIModeration"),
});

builder.Services.AddSingleton(new Dinder.Application.Discovery.Queries.MatchingFeatureFlags
{
    UseMLScoring = builder.Configuration.GetValue<bool>("Matching:UseMLScoring"),
});

// Infrastructure (DB, Auth, JWT, Repos, Stripe)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ── Middleware Pipeline ─────────────────────────────────────────────────────

// Raw body buffering for Stripe webhook signature verification
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1/webhooks/stripe"))
    {
        context.Request.EnableBuffering();
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

// Exception handling — must be early in the pipeline
app.UseMiddleware<Dinder.Api.Middleware.ForbiddenExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Dinder.Infrastructure.Auth.TokenRevocationMiddleware>();
app.MapControllers();

// SignalR hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

// ── Feature Flags ───────────────────────────────────────────────────────────

public sealed class SubscriptionFeatureFlags
{
    public bool EnableSubscriptions { get; init; }
}

public sealed class AiModerationFeatureFlags
{
    public bool UseAIModeration { get; init; }
}
