using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class PromptCatalog
{
    public Guid Id { get; private set; }
    public string Text { get; private set; }
    public PromptCategory Category { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

#pragma warning disable CS8618
    private PromptCatalog() { } // EF Core
#pragma warning restore CS8618

    public PromptCatalog(string text, PromptCategory category)
    {
        Id = Guid.NewGuid();
        Text = text;
        Category = category;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string text, PromptCategory category)
    {
        Text = text;
        Category = category;
    }

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;
}
