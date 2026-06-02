using Dinder.Domain.Interfaces;
using Dinder.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dinder.Infrastructure.Storage;

/// <summary>
/// Stub implementation — returns null (no analysis) until Azure AI Vision integration
/// is implemented in a later phase. This allows the DI container to resolve the service
/// during domain foundation setup.
/// </summary>
public sealed class AzureVisionService : IAzureVisionService
{
    private readonly ILogger<AzureVisionService> _logger;
    private readonly bool _useAIModeration;

    public AzureVisionService(IConfiguration configuration, ILogger<AzureVisionService> logger)
    {
        _logger = logger;
        _useAIModeration = configuration.GetValue<bool>("Azure:UseAIModeration");
    }

    public Task<AIScanResult?> AnalyzeImageAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        if (!_useAIModeration)
        {
            _logger.LogInformation("AI moderation disabled. Skipping analysis for {BlobKey}", blobKey);
            return Task.FromResult<AIScanResult?>(null);
        }

        // Stub: returns null — full Azure AI Vision integration in PR 3
        _logger.LogWarning("Azure AI Vision not yet implemented. Returning null for {BlobKey}", blobKey);
        return Task.FromResult<AIScanResult?>(null);
    }
}
