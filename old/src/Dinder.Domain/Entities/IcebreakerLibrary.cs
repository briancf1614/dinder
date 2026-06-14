using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class IcebreakerLibrary
{
    public Guid Id { get; private set; }
    public string Text { get; private set; }
    public IcebreakerCategory Category { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

#pragma warning disable CS8618
    private IcebreakerLibrary() { } // EF Core
#pragma warning restore CS8618

    public IcebreakerLibrary(string text, IcebreakerCategory category)
    {
        Id = Guid.NewGuid();
        Text = text;
        Category = category;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string text, IcebreakerCategory category)
    {
        Text = text;
        Category = category;
    }

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;
}
