namespace Dinder.Domain.Entities;

/// <summary>
/// Owned entity stored as JSONB on the profiles table.
/// Represents a Hinge-style prompt selection with user's answer.
/// </summary>
public sealed class ProfilePrompt
{
    public Guid PromptId { get; private set; }
    public string Answer { get; private set; }
    public int Order { get; private set; }

#pragma warning disable CS8618
    private ProfilePrompt() { } // EF Core
#pragma warning restore CS8618

    public ProfilePrompt(Guid promptId, string answer, int order)
    {
        PromptId = promptId;
        Answer = answer;
        Order = order;
    }

    public void SetOrder(int order)
    {
        Order = order;
    }
}
