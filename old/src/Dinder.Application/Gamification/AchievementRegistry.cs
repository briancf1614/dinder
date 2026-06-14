using System.Text.Json;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;

namespace Dinder.Application.Gamification;

/// <summary>
/// Loads achievement definitions from achievements.json at startup
/// and serves them as a singleton read-only registry.
/// </summary>
public sealed class AchievementRegistry : IAchievementRegistry
{
    private readonly Dictionary<AchievementType, AchievementDefinition> _definitions;

    public AchievementRegistry()
    {
        _definitions = LoadDefinitions();
    }

    public AchievementDefinition GetDefinition(AchievementType type)
    {
        if (_definitions.TryGetValue(type, out var def))
            return def;

        throw new InvalidOperationException($"Achievement definition not found for {type}");
    }

    public IReadOnlyList<AchievementDefinition> GetAllDefinitions()
    {
        return _definitions.Values.ToList().AsReadOnly();
    }

    private static Dictionary<AchievementType, AchievementDefinition> LoadDefinitions()
    {
        var result = new Dictionary<AchievementType, AchievementDefinition>();

        var basePath = AppContext.BaseDirectory;
        var jsonPath = Path.Combine(basePath, "Gamification", "achievements.json");

        if (!File.Exists(jsonPath))
            return result;

        var json = File.ReadAllText(jsonPath);
        var items = JsonSerializer.Deserialize<List<AchievementJsonItem>>(json);

        if (items is null) return result;

        foreach (var item in items)
        {
            if (Enum.TryParse<AchievementType>(item.Type, out var type))
            {
                result[type] = new AchievementDefinition(
                    type,
                    item.Name,
                    item.Description,
                    item.IconKey,
                    item.Criteria);
            }
        }

        return result;
    }

    private sealed class AchievementJsonItem
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconKey { get; set; } = string.Empty;
        public string Criteria { get; set; } = string.Empty;
    }
}
