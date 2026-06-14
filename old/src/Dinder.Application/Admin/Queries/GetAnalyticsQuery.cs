using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Admin.Queries;

/// <summary>
/// Retrieves aggregate analytics metrics for the admin dashboard.
/// Supports time-range filtering (7/30/90 days).
/// </summary>
public sealed record GetAnalyticsQuery(string Metric, int Days = 30) : IRequest<AnalyticsResult>;

public sealed class GetAnalyticsQueryHandler : IRequestHandler<GetAnalyticsQuery, AnalyticsResult>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IUserRepository _userRepository;

    public GetAnalyticsQueryHandler(
        IAnalyticsRepository analyticsRepository,
        IUserRepository userRepository)
    {
        _analyticsRepository = analyticsRepository;
        _userRepository = userRepository;
    }

    public async Task<AnalyticsResult> Handle(GetAnalyticsQuery request, CancellationToken cancellationToken)
    {
        return request.Metric.ToLowerInvariant() switch
        {
            "dau" => await GetDAUAsync(request.Days, cancellationToken),
            "conversion" => await GetConversionAsync(request.Days, cancellationToken),
            "matches" => await GetMatchMetricsAsync(request.Days, cancellationToken),
            _ => throw new InvalidOperationException(
                $"Unknown metric '{request.Metric}'. Valid metrics: dau, conversion, matches.")
        };
    }

    private async Task<AnalyticsResult> GetDAUAsync(int days, CancellationToken cancellationToken)
    {
        var dauData = await _analyticsRepository.GetDailyActiveUsersAsync(days, cancellationToken);

        var dataPoints = dauData
            .Select(kvp => new AnalyticsDataPoint(kvp.Key.ToString("yyyy-MM-dd"), kvp.Value))
            .OrderBy(dp => dp.Date)
            .ToList();

        return new AnalyticsResult("dau", days, dataPoints);
    }

    private async Task<AnalyticsResult> GetConversionAsync(int days, CancellationToken cancellationToken)
    {
        var dataPoints = new List<AnalyticsDataPoint>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(-i);
            var snapshots = await _analyticsRepository.GetSubscriptionSnapshotAsync(date, cancellationToken);
            var subscribedCount = snapshots.Values.Sum();
            var totalRatio = subscribedCount > 0 ? subscribedCount / (float)Math.Max(subscribedCount, 1) : 0f;

            // Conversion rate = (subscribed users on date) / (total active users on date)
            var dauCount = await _analyticsRepository.GetDailyActiveUserCountAsync(date, cancellationToken);
            var conversionRate = dauCount > 0
                ? (float)Math.Round((double)subscribedCount / dauCount * 100, 2)
                : 0f;

            dataPoints.Add(new AnalyticsDataPoint(date.ToString("yyyy-MM-dd"), conversionRate));
        }

        dataPoints.Reverse(); // Chronological order
        return new AnalyticsResult("conversion", days, dataPoints);
    }

    private async Task<AnalyticsResult> GetMatchMetricsAsync(int days, CancellationToken cancellationToken)
    {
        var dataPoints = new List<AnalyticsDataPoint>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(-i);
            var metrics = await _analyticsRepository.GetSwipeMetricsAsync(date, cancellationToken);

            // Match rate = total matches / total swipes * 100
            float matchRate = 0f;
            if (metrics is not null && metrics.TotalSwipes > 0)
            {
                matchRate = (float)Math.Round(
                    (double)metrics.TotalMatches / metrics.TotalSwipes * 100, 2);
            }

            dataPoints.Add(new AnalyticsDataPoint(date.ToString("yyyy-MM-dd"), matchRate));
        }

        dataPoints.Reverse(); // Chronological order
        return new AnalyticsResult("matches", days, dataPoints);
    }
}

// ── Result types ─────────────────────────────────────────────────────────

public sealed record AnalyticsResult(string Metric, int Days, List<AnalyticsDataPoint> DataPoints);

public sealed record AnalyticsDataPoint(string Date, float Value);
