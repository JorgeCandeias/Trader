using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Tickers
{
    internal interface ITickerProviderGrain : IGrainWithStringKey
    {
        Task<ReactiveResult> GetTickerAsync();

        Task<ReactiveResult?> TryWaitForTickerAsync(Guid version);

        Task<MiniTicker?> TryGetTickerAsync();

        Task SetTickerAsync(MiniTicker item);
    }
}