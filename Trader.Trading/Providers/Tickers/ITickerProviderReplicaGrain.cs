using Orleans;
using Outcompute.Trader.Models;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Tickers
{
    public interface ITickerProviderReplicaGrain : IGrainWithStringKey
    {
        Task<MiniTicker?> TryGetTickerAsync();

        Task SetTickerAsync(MiniTicker item);
    }
}