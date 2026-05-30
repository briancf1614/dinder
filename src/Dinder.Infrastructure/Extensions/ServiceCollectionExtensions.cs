using System.Text;
using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Interfaces;
using Dinder.Infrastructure.Auth;
using Dinder.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Dinder.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContext<DinderDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    DinderDbContext.IdentitySchema);
                npgsqlOptions.UseNetTopologySuite();
            });
        });

        // Auth
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured.");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "Dinder.Api";
        var jwtAudience = configuration["Jwt:Audience"] ?? "Dinder.App";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero // No clock skew — exact 15-min expiry
            };
        });

        services.AddAuthorization();

        // Services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
