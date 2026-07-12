using Dinder.Application.Common.Commands.Auth.Login;
using Dinder.Application.Common.Commands.Auth.Register;
using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Queries.HealthCheck;
using Dinder.Application.Common.Queries.Me;
using Dinder.Infrastructure.Persistence;
using Dinder.Infrastructure.Services;
using FluentValidation;
using Dinder.Application.Common.Behaviors;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ─── MediatR ───
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<HealthCheckQuery>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// ─── EF Core ───
builder.Services.AddDbContext<DinderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ─── DI: enchufes ───
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<DinderDbContext>());
builder.Services.AddScoped<ITokenService, TokenService>();

// ─── FluentValidation ───
builder.Services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();

// ─── Swagger / OpenAPI ───
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dinder API",
        Version = "v1",
        Description = "Dating app API — learning project"
    });

    // Permitir pegar el JWT en Swagger UI para probar endpoints [Authorize]
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegá tu JWT acá (sin 'Bearer ' adelante, Swagger lo agrega solo)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

// Swagger UI (solo en Development por seguridad)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dinder API v1");
    });
}

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
