namespace Dinder.Domain.ValueObjects;

/// <summary>A profile with its computed similarity score for ML-based ranking.</summary>
public sealed record ScoredProfile(Guid ProfileId, double Score);
