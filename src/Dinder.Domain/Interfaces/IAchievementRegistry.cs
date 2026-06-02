using Dinder.Domain.Enums;

namespace Dinder.Domain.Interfaces;

/// <summary>Data-driven achievement definition lookup.</summary>
public interface IAchievementRegistry
{
    /// <summary>Get the definition for a given achievement type.</summary>
    AchievementDefinition GetDefinition(AchievementType type);

    /// <summary>Get all registered achievement definitions.</summary>
    IReadOnlyList<AchievementDefinition> GetAllDefinitions();
}

/// <summary>Immutable definition of an achievement badge.</summary>
public sealed record AchievementDefinition(
    AchievementType Type,
    string Name,
    string Description,
    string IconKey,
    string UnlockCriteria);
