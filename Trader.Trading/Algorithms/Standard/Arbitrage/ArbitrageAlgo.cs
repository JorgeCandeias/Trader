using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Arbitrage;

internal partial class ArbitrageAlgo : Algo
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

    private static string TypeName => nameof(ArbitrageAlgo);

    protected override async ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
    {
        var pools = await _swaps.GetSwapPoolConfigurationsAsync(cancellationToken);

        foreach (var pool in pools)
        {
            var assets = pool.Assets.Select(x => x.Key).ToArray();

            // get the symbol info
            var name1 = $"{assets[0]}{assets[1]}";
            var name2 = $"{assets[1]}{assets[0]}";

            var symbol =
                _info.TryGetSymbol(name1) ??
                _info.TryGetSymbol(name2);

            if (symbol is null)
            {
                LogCouldNotFindSymbol(TypeName, name1, name2);

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
                LogFoundArbitrageOpportunityBuyingSpot(TypeName, symbol.Name, ticker.Price, comparable, relative);
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
                LogFoundArbitrageOpportunitySwapping(TypeName, symbol.Name, comparable, ticker.Price, relative);
            }
        }
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{TypeName} could not find symbol for {Name1} nor {Name2}")]
    private partial void LogCouldNotFindSymbol(string typeName, string name1, string name2);

    [LoggerMessage(1, LogLevel.Information, "{TypeName} found arbitrage opportunity for symbol {Symbol} by buying spot at {Buy:F8} and swapping at {Sell:F8} for a profit of {Profit:P2}")]
    private partial void LogFoundArbitrageOpportunityBuyingSpot(string typeName, string symbol, decimal buy, decimal sell, decimal profit);

    [LoggerMessage(2, LogLevel.Information, "{TypeName} found arbitrage opportunity for symbol {Symbol} by swapping at {Buy:F8} and selling spot at {Sell:F8} for a profit of {Profit:P2}")]
    private partial void LogFoundArbitrageOpportunitySwapping(string typeName, string symbol, decimal buy, decimal sell, decimal profit);

    #endregion Logging
}