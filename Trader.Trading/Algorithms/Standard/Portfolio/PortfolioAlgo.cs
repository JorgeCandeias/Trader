using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Portfolio;

public partial class PortfolioAlgo : Algo
{
    private readonly AlgoOptions _host;
    private readonly PortfolioAlgoOptions _options;
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;

    public PortfolioAlgo(IOptionsSnapshot<AlgoOptions> host, IOptionsSnapshot<PortfolioAlgoOptions> options, ILogger<PortfolioAlgo> logger, ISystemClock clock)
    {
        _host = host.Get(Context.Name);
        _options = options.Get(Context.Name);
        _logger = logger;
        _clock = clock;
    }

    private const string TypeName = nameof(PortfolioAlgo);

    protected override IAlgoCommand OnExecute()
    {
        if (TryElectTopUpBuy(out var elected))
        {
            // do something
        }

        if (TryElectEntryBuy(out elected))
        {
            // do something
        }

        return Noop();
    }

    private bool TryElectEntryBuy(out SymbolData elected)
    {
        elected = null!;
        var lastRsi = decimal.MaxValue;

        // evaluate all symbols with no positions
        foreach (var item in Context.Data.Where(x => x.AutoPosition.Positions.Count == 0))
        {
            // evaluate the rsi for the symbol
            var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Periods);
            if (rsi <= _options.Rsi.Oversold && rsi < lastRsi)
            {
                elected = item;
                lastRsi = rsi;
            }
        }

        if (elected is null)
        {
            return false;
        }

        LogSelectedSymbolForEntryBuy(TypeName, elected.Name, elected.Ticker.ClosePrice, elected.Symbol.QuoteAsset, lastRsi);
        return true;
    }

    private bool TryElectTopUpBuy(out SymbolData elected)
    {
        elected = null!;
        var lastRelPnL = decimal.MinValue;
        var lastRsi = decimal.MaxValue;

        var cooldown = _clock.UtcNow.Subtract(_options.Cooldown);

        // evaluate symbols with at least one position
        foreach (var item in Context.Data.Where(x => x.AutoPosition.Positions.Count > 0))
        {
            // skip symbols on cooldown
            if (item.AutoPosition.Positions.Max!.Time >= cooldown)
            {
                continue;
            }

            // evaluate pnl
            var cost = item.AutoPosition.Positions.Sum(x => x.Quantity * x.Price);
            var pv = item.AutoPosition.Positions.Sum(x => x.Quantity * item.Ticker.ClosePrice);
            var absPnL = pv - cost;
            var relPnL = absPnL / cost;

            // skip symbols below min required for top up
            if (relPnL < _options.MinRequiredRelativePnLForTopUpBuy)
            {
                continue;
            }

            // skip symbols below the highest candidate yet
            if (elected is not null && relPnL <= lastRelPnL)
            {
                continue;
            }

            // evaluate the rsi for the symbol
            var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Periods);

            // skip symbols with not low enough rsi
            if (rsi > _options.Rsi.Oversold)
            {
                continue;
            }

            // skip symbols with rsi not lower than the highest candidate yet
            if (elected is not null && rsi > lastRsi)
            {
                continue;
            }

            // if we got here then we have a new candidate
            elected = item;
            lastRelPnL = relPnL;
            lastRsi = rsi;
        }

        if (elected is null)
        {
            return false;
        }

        LogSelectedSymbolForTopUpBuy(TypeName, elected.Name, elected.Ticker.ClosePrice, elected.Symbol.QuoteAsset, lastRelPnL, lastRsi);
        return true;
    }

    /*
    private bool TryElectPanicSell(out SymbolData elected)
    {
        // evaluate symbols with at least two positions
        foreach (var item in Context.Data.Where(x => x.AutoPosition.Orders.Count >= 2))
        {
            // calculate stats
        }
    }
    */

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} selected symbol {Symbol} for entry buy with ticker {Ticker:F8} {Quote} and RSI {Rsi:F8}")]
    private partial void LogSelectedSymbolForEntryBuy(string type, string symbol, decimal ticker, string quote, decimal rsi);

    [LoggerMessage(1, LogLevel.Information, "{Type} selected symbol {Symbol} for top up buy with ticker {Ticker:F8} {Quote}, RelPnL = {RelPnL:P8}, RSI {Rsi:F8}")]
    private partial void LogSelectedSymbolForTopUpBuy(string type, string symbol, decimal ticker, string quote, decimal relPnL, decimal rsi);

    #endregion Logging
}