using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Media.Commands;

public sealed record GenerateUploadUrlCommand(Guid UserId, string ContentType, string Extension) : IRequest<UploadUrlResult>;

public sealed record UploadUrlResult(string UploadUrl, string BlobKey, DateTimeOffset ExpiresAt);

public sealed class GenerateUploadUrlCommandHandler : IRequestHandler<GenerateUploadUrlCommand, UploadUrlResult>
{
    private readonly IBlobStorageService _blobStorage;
    private readonly IProfileRepository _profileRepository;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "webp"
    };

    public GenerateUploadUrlCommandHandler(
        IBlobStorageService blobStorage,
        IProfileRepository profileRepository)
    {
        _blobStorage = blobStorage;
        _profileRepository = profileRepository;
    }

    public async Task<UploadUrlResult> Handle(GenerateUploadUrlCommand request, CancellationToken cancellationToken)
    {
        // Validate extension
        if (!AllowedExtensions.Contains(request.Extension))
            throw new InvalidOperationException($"Extension '{request.Extension}' not allowed. Must be: {string.Join(", ", AllowedExtensions)}");

        // Enforce ≤6 photos limit at upload-URL request time
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is not null)
        {
            var photoCount = await _profileRepository.GetPhotoCountAsync(profile.Id, cancellationToken);
            if (photoCount >= 6)
                throw new InvalidOperationException("Maximum 6 photos allowed per profile.");
        }

        var uploadUrl = await _blobStorage.GenerateUploadUrlAsync(request.UserId, request.Extension, request.ContentType, cancellationToken);

        // Extract blob key from SAS URL
        var uri = new Uri(uploadUrl);
        var blobKey = uri.AbsolutePath.TrimStart('/');
        // SAS URI has the full path; extract the blob key (after container name)
        var pathParts = blobKey.Split('/');
        var containerIndex = Array.FindIndex(pathParts, p => p == "dinder-photos");
        if (containerIndex >= 0)
            blobKey = string.Join("/", pathParts[(containerIndex + 1)..]);

        return new UploadUrlResult(uploadUrl, blobKey, DateTimeOffset.UtcNow.AddMinutes(10));
    }
}
