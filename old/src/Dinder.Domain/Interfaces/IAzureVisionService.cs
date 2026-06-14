using Dinder.Domain.ValueObjects;

namespace Dinder.Domain.Interfaces;

public interface IAzureVisionService
{
    /// <summary>
    /// Analyzes an image stored in Azure Blob for adult, racy, and violence content.
    /// Returns null if the image cannot be analyzed (e.g., not found, unsupported format).
    /// </summary>
    Task<AIScanResult?> AnalyzeImageAsync(string blobKey, CancellationToken cancellationToken = default);
}
