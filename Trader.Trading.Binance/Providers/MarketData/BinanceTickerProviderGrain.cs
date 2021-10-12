using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    /// <summary>
    /// This grain actively pulls the ticker information for a given symbol into the silos that need it.
    /// This allows algos to query the latest ticker information without network latency.
    /// </summary>
    [StatelessWorker(1)]
    internal class BinanceTickerProviderGrain : Grain, IBinanceTickerProviderGrain
    {
        private readonly BinanceOptions _options;
        private readonly IBinanceMarketDataGrain _market;

        public BinanceTickerProviderGrain(IOptions<BinanceOptions> options, IGrainFactory factory)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _market = factory?.GetBinanceMarketDataGrain() ?? throw new ArgumentNullException(nameof(factory));
        }

        private string _symbol = Empty;

        private MiniTicker? _ticker;

        public override async Task OnActivateAsync()
        {
            _symbol = this.GetPrimaryKeyString();

            await TickUpdateAsync();

            RegisterTimer(TickUpdateAsync, null, _options.TickerBroadcastDelay, _options.TickerBroadcastDelay);

            await base.OnActivateAsync();
        }

        private async Task TickUpdateAsync(object? _ = default)
        {
            _ticker = await _market.TryGetTickerAsync(_symbol);
        }

        public ValueTask<MiniTicker?> TryGetTickerAsync() => new(_ticker);
    }
}