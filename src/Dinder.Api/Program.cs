using Dinder.Application.Common.Queries.HealthCheck;
using MediatR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HealthCheckQuery>());

var app = builder.Build();

app.MapGet("/health", async (IMediator mediator) =>
{
    var result = await mediator.Send(new HealthCheckQuery());
    return Results.Ok(result);
});
app.MapGet("/", () => "Dinder API running!");
app.Run();
