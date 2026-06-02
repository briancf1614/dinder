using Dinder.Domain.Enums;

namespace Dinder.Application.Common.Interfaces;

public interface IStripePriceResolver
{
    string GetPriceId(SubscriptionTier tier);
}
