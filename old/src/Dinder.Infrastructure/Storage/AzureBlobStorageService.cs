using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Dinder.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Dinder.Infrastructure.Storage;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly string _cdnBaseUrl;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["Azure:BlobStorage:ConnectionString"]
            ?? "UseDevelopmentStorage=true";
        _blobServiceClient = new BlobServiceClient(connectionString);

        _containerName = configuration["Azure:BlobStorage:ContainerName"] ?? "dinder-photos";
        _cdnBaseUrl = configuration["Azure:CDN:BaseUrl"] ?? "https://cdn.dinder.local/photos";

        // Ensure container exists
        _blobServiceClient.GetBlobContainerClient(_containerName).CreateIfNotExists();
    }

    public async Task<string> GenerateUploadUrlAsync(Guid userId, string extension, string contentType, CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException($"Content type '{contentType}' is not allowed. Must be one of: {string.Join(", ", AllowedContentTypes)}");

        var blobKey = $"users/{userId}/photos/{Guid.NewGuid()}.{extension}";
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobKey);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobKey,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        // For development storage (Azurite), we use the account key approach
        // In production, use UserDelegationKey
        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return await Task.FromResult(sasUri.ToString());
    }

    public async Task<bool> BlobExistsAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobKey);
        return await blobClient.ExistsAsync(cancellationToken);
    }

    public string GetCdnUrl(string blobKey)
    {
        return $"{_cdnBaseUrl}/{blobKey}";
    }

    public async Task DeleteBlobAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobKey);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteUserBlobsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var prefix = $"users/{userId}/photos/";
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }
}
