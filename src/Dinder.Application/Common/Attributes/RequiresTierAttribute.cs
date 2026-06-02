using Dinder.Domain.Enums;

namespace Dinder.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RequiresTierAttribute : Attribute
{
    public SubscriptionTier MinimumTier { get; }

    public RequiresTierAttribute(SubscriptionTier minimumTier)
    {
        MinimumTier = minimumTier;
    }
}
