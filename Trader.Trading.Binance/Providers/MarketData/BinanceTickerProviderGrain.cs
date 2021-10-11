using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    /// <summary>
    /// This grain actively pulls the ticker information for a given symbol into the silos that needs it.
    /// This allows algos to query the latest ticker information without network latency.
    /// </summary>
    [StatelessWorker(1)]
    internal class BinanceTickerProviderGrain : Grain, IBinanceTickerProviderGrain
    {
        private readonly IBinanceMarketDataGrain _market;

        public BinanceTickerProviderGrain(IGrainFactory factory)
        {
            _market = factory.GetBinanceMarketDataGrain();
        }

        private string _symbol = Empty;

        private MiniTicker? _ticker;
        private Guid _version;

        public override async Task OnActivateAsync()
        {
            _symbol = this.GetPrimaryKeyString();

            await TickUpdateAsync();

            RegisterTimer(_ => TickUpdateAsync(), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

            await base.OnActivateAsync();
        }

        private async Task TickUpdateAsync()
        {
            (_ticker, _version) = await _market.LongPollTickerAsync(_symbol, _version);
        }

        public ValueTask<MiniTicker?> TryGetTickerAsync() => new(_ticker);
    }
}