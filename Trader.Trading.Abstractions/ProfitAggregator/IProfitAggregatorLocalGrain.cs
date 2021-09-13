using Orleans;
using Outcompute.Trader.Trading.Algorithms;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.ProfitAggregator
{
    public interface IProfitAggregatorLocalGrain : IGrainWithGuidKey
    {
        Task PublishAsync(Profit profit);
    }
}