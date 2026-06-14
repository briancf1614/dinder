using Dinder.Application.Common.Models;
using MediatR;

namespace Dinder.Application.Common.Queries.HealthCheck;
// Esto es el "mensaje". Solo dice "quiero un health check".
// Implementa IRequest<T> para decirle a MediatR qué tipo de respuesta esperamos.
public class HealthCheckQuery : IRequest<HealthCheckResult>
{
}