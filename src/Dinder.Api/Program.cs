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

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Dinder.Application.Identity.Commands.RegisterCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Dinder.Application.Identity.Commands.RegisterCommand).Assembly);

// SignalR
builder.Services.AddSignalR();

// Infrastructure (DB, Auth, JWT, Repos)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ── Middleware Pipeline ─────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Dinder.Infrastructure.Auth.TokenRevocationMiddleware>();
app.MapControllers();

// SignalR hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
