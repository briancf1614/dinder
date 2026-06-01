namespace Dinder.Domain.Interfaces;

/// <summary>Abstraction for blob storage operations (pre-signed URLs, CDN, deletion).</summary>
public interface IBlobStorageService
{
    /// <summary>Generate a pre-signed PUT URL for direct client-to-blob upload.</summary>
    Task<string> GenerateUploadUrlAsync(Guid userId, string extension, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Verify that a blob exists in storage.</summary>
    Task<bool> BlobExistsAsync(string blobKey, CancellationToken cancellationToken = default);

    /// <summary>Get the CDN URL for an approved blob.</summary>
    string GetCdnUrl(string blobKey);

    /// <summary>Delete a blob from storage.</summary>
    Task DeleteBlobAsync(string blobKey, CancellationToken cancellationToken = default);

    /// <summary>Delete all blobs for a user (GDPR cascade).</summary>
    Task DeleteUserBlobsAsync(Guid userId, CancellationToken cancellationToken = default);
}
