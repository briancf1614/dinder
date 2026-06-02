using Dinder.Domain.Interfaces;
using Dinder.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dinder.Infrastructure.Storage;

/// <summary>
/// Azure AI Vision REST v3.2 integration for photo content moderation.
/// Supports real API calls when credentials are configured, and falls back
/// to simulated analysis for local development when no credentials exist.
/// </summary>
public sealed class AzureVisionService : IAzureVisionService
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<AzureVisionService> _logger;
    private readonly bool _useAIModeration;
    private readonly string? _endpoint;
    private readonly string? _apiKey;
    private readonly float _adultThreshold;
    private readonly float _racyThreshold;
    private readonly float _violenceThreshold;

    public AzureVisionService(
        IConfiguration configuration,
        IBlobStorageService blobStorage,
        ILogger<AzureVisionService> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _blobStorage = blobStorage;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _useAIModeration = configuration.GetValue<bool>("Azure:UseAIModeration");
        _endpoint = configuration["Azure:ComputerVision:Endpoint"];
        _apiKey = configuration["Azure:ComputerVision:ApiKey"];
        _adultThreshold = configuration.GetValue("Azure:ComputerVision:AdultThreshold", 0.5f);
        _racyThreshold = configuration.GetValue("Azure:ComputerVision:RacyThreshold", 0.7f);
        _violenceThreshold = configuration.GetValue("Azure:ComputerVision:ViolenceThreshold", 0.5f);
    }

    public async Task<AIScanResult?> AnalyzeImageAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        if (!_useAIModeration)
        {
            _logger.LogInformation("AI moderation disabled. Skipping analysis for {BlobKey}", blobKey);
            return null;
        }

        // Check if real Azure credentials are configured
        if (!string.IsNullOrWhiteSpace(_endpoint) && !string.IsNullOrWhiteSpace(_apiKey))
        {
            return await AnalyzeWithAzureAsync(blobKey, cancellationToken);
        }

        // No credentials — use simulated analysis for development
        return await AnalyzeSimulatedAsync(blobKey);
    }

    // ── Real Azure AI Vision API ─────────────────────────────────────────

    private async Task<AIScanResult?> AnalyzeWithAzureAsync(string blobKey, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory?.CreateClient("AzureVision")
                ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            var url = _endpoint!.TrimEnd('/') + "/vision/v3.2/analyze?visualFeatures=Adult";
            var imageUrl = _blobStorage.GetCdnUrl(blobKey);

            var requestBody = System.Text.Json.JsonSerializer.Serialize(new { url = imageUrl });

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            _logger.LogDebug("Calling Azure AI Vision for {BlobKey}", blobKey);

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Azure AI Vision returned {StatusCode} for {BlobKey}: {Error}",
                    (int)response.StatusCode, blobKey, errorBody);
                return null;
            }

            using var jsonDoc = await System.Text.Json.JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

            var adult = jsonDoc.RootElement.GetProperty("adult");

            var adultScore = adult.GetProperty("adultScore").GetSingle();
            var racyScore = adult.GetProperty("racyScore").GetSingle();
            var goreScore = adult.TryGetProperty("goreScore", out var gs) ? gs.GetSingle() : 0f;

            var isAdult = adult.GetProperty("isAdultContent").GetBoolean();
            var isRacy = adult.GetProperty("isRacyContent").GetBoolean();
            var isGory = adult.TryGetProperty("isGoryContent", out var ig) && ig.GetBoolean();

            _logger.LogInformation(
                "Azure AI Vision analysis for {BlobKey}: Adult={AdultScore:F3}, Racy={RacyScore:F3}, Gore={GoreScore:F3}",
                blobKey, adultScore, racyScore, goreScore);

            return new AIScanResult(
                AdultScore: adultScore,
                RacyScore: racyScore,
                ViolenceScore: goreScore,
                IsAdultContent: isAdult || adultScore >= _adultThreshold,
                IsRacyContent: isRacy || racyScore >= _racyThreshold,
                IsGoryContent: isGory || goreScore >= _violenceThreshold);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Azure AI Vision HTTP error for {BlobKey}: {Message}", blobKey, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AI Vision unexpected error for {BlobKey}: {Message}", blobKey, ex.Message);
            return null;
        }
    }

    // ── Simulated analysis (dev / no credentials) ────────────────────────

    private Task<AIScanResult?> AnalyzeSimulatedAsync(string blobKey)
    {
        _logger.LogInformation(
            "Azure AI Vision credentials not configured. Using simulated analysis for {BlobKey}", blobKey);

        return Task.Run<AIScanResult?>(async () =>
        {
            // Simulate realistic network latency (50-200ms)
            await Task.Delay(Random.Shared.Next(50, 200));

            // Simulate ~85% clean, ~15% flagged distribution
            var isFlagged = Random.Shared.NextDouble() < 0.15;

            if (!isFlagged)
            {
                return new AIScanResult(
                    AdultScore: (float)(Random.Shared.NextDouble() * 0.1),
                    RacyScore: (float)(Random.Shared.NextDouble() * 0.2),
                    ViolenceScore: (float)(Random.Shared.NextDouble() * 0.05),
                    IsAdultContent: false,
                    IsRacyContent: false,
                    IsGoryContent: false);
            }

            // Flagged — random high score in one category
            var category = Random.Shared.Next(3);
            return new AIScanResult(
                AdultScore: category == 0 ? (float)(0.7 + Random.Shared.NextDouble() * 0.3) : (float)(Random.Shared.NextDouble() * 0.2),
                RacyScore: category == 1 ? (float)(0.7 + Random.Shared.NextDouble() * 0.3) : (float)(Random.Shared.NextDouble() * 0.2),
                ViolenceScore: category == 2 ? (float)(0.7 + Random.Shared.NextDouble() * 0.3) : (float)(Random.Shared.NextDouble() * 0.05),
                IsAdultContent: category == 0,
                IsRacyContent: category == 1,
                IsGoryContent: category == 2);
        });
    }
}
