using Orleans;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.ProfitAggregator;

public interface IProfitAggregatorGrain : IGrainWithGuidKey
{
    Task PublishAsync(IEnumerable<Profit> profits);

    Task<IEnumerable<Profit>> GetProfitsAsync();
}