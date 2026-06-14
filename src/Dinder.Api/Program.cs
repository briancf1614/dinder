using Dinder.Application.Common.Queries.HealthCheck;
using Dinder.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HealthCheckQuery>());
// EF Core — lee el connection string de appsettings.json
builder.Services.AddDbContext<DinderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.MapGet("/health", async (IMediator mediator) =>
{
    var result = await mediator.Send(new HealthCheckQuery());
    return Results.Ok(result);
});
app.MapGet("/", () => "Dinder API running!");
app.Run();
