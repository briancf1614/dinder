using MediatR;

namespace Dinder.Domain.Events;

public sealed record PhotoUploadedEvent(Guid MediaFileId, Guid OwnerId, string BlobKey) : INotification;
