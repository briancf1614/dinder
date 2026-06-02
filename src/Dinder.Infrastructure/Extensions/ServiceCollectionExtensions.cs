using System.Text;
using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Interfaces;
using Dinder.Infrastructure.Auth;
using Dinder.Application.Gamification;
using Dinder.Infrastructure.Matching;
using Dinder.Infrastructure.Payments;
using Dinder.Infrastructure.Persistence;
using Dinder.Infrastructure.Storage;
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

        services.AddDbContext<ProfileDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    ProfileDbContext.ProfileSchema);
                npgsqlOptions.UseNetTopologySuite();
            });
        });

        services.AddDbContext<DiscoveryDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    DiscoveryDbContext.DiscoverySchema);
                npgsqlOptions.UseNetTopologySuite();
            });
        });

        services.AddDbContext<CommunicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    CommunicationDbContext.CommunicationSchema);
                npgsqlOptions.UseNetTopologySuite();
            });
        });

        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    NotificationDbContext.NotificationSchema);
                npgsqlOptions.UseNetTopologySuite();
            });
        });

        services.AddDbContext<ModerationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    ModerationDbContext.ModerationSchema);
                npgsqlOptions.UseNetTopologySuite();
            });
        });

        services.AddDbContext<AdminDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    AdminDbContext.AdminSchema);
                npgsqlOptions.UseNetTopologySuite();
            });
        });

        services.AddDbContext<MediaDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    MediaDbContext.MediaSchema);
                npgsqlOptions.UseNetTopologySuite();
            });
        });

        services.AddDbContext<SubscriptionDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    SubscriptionDbContext.SubscriptionSchema);
            });
        });

        services.AddDbContext<AnalyticsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    AnalyticsDbContext.AnalyticsSchema);
            });
        });

        // Stripe
        services.Configure<StripeConfiguration>(configuration.GetSection(StripeConfiguration.SectionName));

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

            // Enable JWT auth for SignalR via query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        // Services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IDiscoveryRepository, DiscoveryRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IModerationRepository, ModerationRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IStripeService, StripeService>();
        services.AddSingleton<IStripePriceResolver, StripePriceResolver>();

        // Blob storage
        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

        // AI moderation
        services.AddSingleton<IAzureVisionService, AzureVisionService>();

        // Analytics
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

        // Gamification
        services.AddSingleton<IAchievementRegistry, AchievementRegistry>();
        services.AddSingleton<IProfileScorer, MlNetProfileScorer>();

        return services;
    }
}
