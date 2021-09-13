using Orleans;
using Outcompute.Trader.Trading.Algorithms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.ProfitAggregator
{
    public interface IProfitAggregatorGrain : IGrainWithGuidKey
    {
        Task PublishAsync(IEnumerable<Profit> profits);

        ValueTask<IEnumerable<Profit>> GetProfitsAsync();
    }
}