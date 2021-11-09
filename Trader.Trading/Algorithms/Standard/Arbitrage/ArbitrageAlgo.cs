using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Arbitrage
{
    internal class ArbitrageAlgo : SymbolAlgo
    {
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly ISwapPoolProvider _swaps;
        private readonly IExchangeInfoProvider _info;

        public ArbitrageAlgo(ILogger<ArbitrageAlgo> logger, ITradingService trader, ISwapPoolProvider swaps, IExchangeInfoProvider info)
        {
            _logger = logger;
            _trader = trader;
            _swaps = swaps;
            _info = info;
        }

        protected override async Task<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            var pools = await _swaps.GetSwapPoolConfigurationsAsync(cancellationToken);

            foreach (var pool in pools)
            {
                var assets = pool.Assets.Select(x => x.Key).ToArray();

                // get the symbol info
                var name1 = $"{assets[0]}{assets[1]}";
                var name2 = $"{assets[1]}{assets[0]}";

                var symbol =
                    await _info.TryGetSymbolAsync(name1, cancellationToken) ??
                    await _info.TryGetSymbolAsync(name2, cancellationToken);

                if (symbol is null)
                {
                    _logger.LogInformation("Could not find symbol for {Name1} nor {Name2}", name1, name2);

                    continue;
                }

                await TryExchangeToSwapAsync(pool, symbol, cancellationToken);
                await TrySwapToExchangeAsync(pool, symbol, cancellationToken);
            }

            return Noop();
        }

        private async Task TryExchangeToSwapAsync(SwapPoolConfiguration pool, Symbol symbol, CancellationToken cancellationToken)
        {
            // get the price on the exchange
            var tickerTask = _trader.GetSymbolPriceTickerAsync(symbol.Name, cancellationToken);

            // get the preview on the swap
            var quoteTask = _trader.GetSwapPoolQuoteAsync(symbol.BaseAsset, symbol.QuoteAsset, pool.Assets[symbol.BaseAsset].MinSwap, cancellationToken);

            // wait for both
            var ticker = await tickerTask.ConfigureAwait(false);
            var quote = await quoteTask.ConfigureAwait(false);

            // calculate the absolute diff
            var comparable = 1m / quote.Price;
            var diff = comparable - ticker.Price;
            if (diff > 0)
            {
                // calculate the relative diff
                var relative = diff / ticker.Price;

                if (relative >= 0.005m)
                {
                    _logger.LogInformation(
                        "{Type} found arbitrage opportunity for symbol {Symbol} by buying spot at {Buy:F8} and swapping at {Sell:F8} for a profit of {Profit:P2}",
                        nameof(ArbitrageAlgo), symbol.Name, ticker.Price, comparable, relative);
                }
            }
        }

        private async Task TrySwapToExchangeAsync(SwapPoolConfiguration pool, Symbol symbol, CancellationToken cancellationToken)
        {
            // get the preview on the swap
            var quoteTask = _trader.GetSwapPoolQuoteAsync(symbol.QuoteAsset, symbol.BaseAsset, pool.Assets[symbol.QuoteAsset].MinSwap, cancellationToken);

            // get the price on the exchange
            var tickerTask = _trader.GetSymbolPriceTickerAsync(symbol.Name, cancellationToken);

            // wait for both
            var ticker = await tickerTask.ConfigureAwait(false);
            var quote = await quoteTask.ConfigureAwait(false);

            // calculate the absolute diff
            var comparable = quote.Price;
            var diff = ticker.Price - comparable;
            if (diff > 0)
            {
                // calculate the relative diff
                var relative = diff / comparable;

                if (relative >= 0.005m)
                {
                    _logger.LogInformation(
                        "{Type} found arbitrage opportunity for symbol {Symbol} by swapping at {Buy:F8} and selling spot at {Sell:F8} for a profit of {Profit:P2}",
                        nameof(ArbitrageAlgo), symbol.Name, comparable, ticker.Price, relative);
                }
            }
        }
    }
}