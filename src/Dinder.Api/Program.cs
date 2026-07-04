using Dinder.Application.Common.Commands.Auth.Login;
using Dinder.Application.Common.Commands.Auth.Register;
using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Queries.HealthCheck;
using Dinder.Application.Common.Queries.Me;
using Dinder.Infrastructure.Persistence;
using Dinder.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ─── MediatR ───
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HealthCheckQuery>());

// ─── EF Core ───
builder.Services.AddDbContext<DinderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ─── DI: enchufes ───
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<DinderDbContext>());
builder.Services.AddScoped<ITokenService, TokenService>();

// ─── FluentValidation ───
builder.Services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();

// ─── JWT Auth ───
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secret = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secret)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ─── Middleware ───
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (IMediator mediator) =>
{
    var result = await mediator.Send(new HealthCheckQuery());
    return Results.Ok(result);
});

app.MapPost("/auth/login", async (LoginCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return Results.Ok(result);
});
app.MapPost("/auth/refresh", async (RefreshCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return Results.Ok(result);
});
app.MapGet("/me", [Microsoft.AspNetCore.Authorization.Authorize] async (IMediator mediator) =>
{
    var result = await mediator.Send(new MeQuery());
    return Results.Ok(result);
});

app.MapGet("/", () => "Dinder API running!");

app.MapPost("/auth/register", async (RegisterCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return Results.Ok(result);
});

app.Run();
