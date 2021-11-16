using Orleans;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.ProfitAggregator;

public interface IProfitAggregatorLocalGrain : IGrainWithGuidKey
{
    Task PublishAsync(Profit profit);
}