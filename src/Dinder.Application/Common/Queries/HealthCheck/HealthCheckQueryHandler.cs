using Dinder.Application.Common.Models;
using MediatR;

namespace Dinder.Application.Common.Queries.HealthCheck;
// El handler es quien realmente hace el trabajo.
// MediatR busca automáticamente cualquier IRequestHandler<Query, Resultado> y lo ejecuta.
public class HealthCheckQueryHandler : IRequestHandler<HealthCheckQuery, HealthCheckResult>
{
    public Task<HealthCheckResult> Handle(HealthCheckQuery request, CancellationToken cancellationToken)
    {
        var result = new HealthCheckResult
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow
        };
        return Task.FromResult(result);
    }
}