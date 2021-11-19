using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using System.Buffers;

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
        if (TryElectTopUpBuy(out var elected) || TryElectEntryBuy(out elected))
        {
            return CreateBuy(elected);
        }

        if (TryElectPanicSell(out var items))
        {
            var commands = new List<IAlgoCommand>(items.Length);

            foreach (var item in items)
            {
                commands.Add(AveragingSell(item.Symbol, _options.MinSellRate, _options.UseSavings, _options.UseSwapPools));
            }

            return Sequence(commands);
        }

        return Noop();
    }

    private bool TryElectEntryBuy(out SymbolData elected)
    {
        LogEvaluatingSymbolsForEntryBuy(TypeName);

        elected = null!;
        var lastRsi = decimal.MaxValue;

        foreach (var item in Context.Data)
        {
            // evaluate pnl
            var stats = item.AutoPosition.Positions.GetStats(item.Ticker.ClosePrice);

            // only look at symbols under the min lot size or min notional
            if (!(stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity || stats.TotalCost < item.Symbol.Filters.MinNotional.MinNotional))
            {
                LogSkippedSymbolWithQuantityAndNotionalAboveMin(TypeName, item.Symbol.Name, stats.TotalQuantity, item.Symbol.Filters.LotSize.MinQuantity, item.Symbol.BaseAsset, stats.TotalCost, item.Symbol.Filters.MinNotional.MinNotional, item.Symbol.QuoteAsset);
                continue;
            }

            // evaluate the rsi for the symbol
            var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Periods);

            // skip symbol with rsi above oversold
            if (rsi > _options.Rsi.Oversold)
            {
                LogSkippedSymbolWithRsiAboveOversold(TypeName, item.Symbol.Name, _options.Rsi.Periods, rsi, _options.Rsi.Oversold);
                continue;
            }

            // skip symbol with rsi above current candidate
            if (elected is not null && rsi > lastRsi)
            {
                LogSkippedSymbolWithRsiAboveCandidate(TypeName, item.Symbol.Name, _options.Rsi.Periods, rsi, lastRsi, elected.Symbol.Name);
                continue;
            }

            // if we got here then we have a new candidate
            elected = item;
            lastRsi = rsi;
            LogSelectedNewCandidateForEntryBuy(TypeName, item.Symbol.Name, _options.Rsi.Periods, rsi);
        }

        if (elected is null)
        {
            return false;
        }

        LogElectedSymbolForEntryBuy(TypeName, elected.Name, elected.Ticker.ClosePrice, elected.Symbol.QuoteAsset, lastRsi);
        return true;
    }

    private bool TryElectTopUpBuy(out SymbolData elected)
    {
        LogEvaluatingSymbolsForTopUpBuy(TypeName);

        elected = null!;
        var highRelValue = decimal.MinValue;
        var lowRsi = decimal.MaxValue;
        var now = _clock.UtcNow;

        // evaluate symbols with at least one position
        foreach (var item in Context.Data)
        {
            // evaluate pnl
            var stats = item.AutoPosition.Positions.GetStats(item.Ticker.ClosePrice);

            // skip symbols under the min lot size - leftovers are handled by the entry signal
            if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                LogSkippedSymbolWithQuantityUnderMinLotSize(TypeName, item.Symbol.Name, stats.TotalQuantity, item.Symbol.Filters.LotSize.MinQuantity);
                continue;
            }

            // skip symbols under the min notional - leftovers are handled by the entry signal
            if (stats.TotalCost < item.Symbol.Filters.MinNotional.MinNotional)
            {
                LogSkippedSymbolWithCostUnderMinNotional(TypeName, item.Symbol.Name, stats.TotalCost, item.Symbol.Filters.MinNotional.MinNotional);
                continue;
            }

            // skip symbols on cooldown
            var cooldown = item.AutoPosition.Positions.Last.Time.Add(_options.Cooldown);
            if (cooldown > now)
            {
                LogSkippedSymbolOnCooldown(TypeName, item.Symbol.Name, cooldown);
                continue;
            }

            // skip symbols below min required for top up
            if (stats.RelativeValue < _options.MinRequiredRelativeValueForTopUpBuy)
            {
                LogSkippedSymbolWithLowRelativeValue(TypeName, item.Symbol.Name, stats.RelativeValue, _options.MinRequiredRelativeValueForTopUpBuy);
                continue;
            }

            // skip symbols below the highest candidate yet
            if (elected is not null && stats.RelativeValue <= highRelValue)
            {
                LogSkippedSymbolWithLowerRelativeValueThanCandidate(TypeName, item.Symbol.Name, stats.RelativeValue, highRelValue, elected.Symbol.Name);
                continue;
            }

            // evaluate the rsi for the symbol
            var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Periods);

            // skip symbols with not low enough rsi
            if (rsi > _options.Rsi.Oversold)
            {
                LogSkippedSymbolWithRsiAboveOversold(TypeName, item.Symbol.Name, _options.Rsi.Periods, rsi, _options.Rsi.Oversold);
                continue;
            }

            // skip symbols with rsi not lower than the highest candidate yet
            if (elected is not null && rsi > lowRsi)
            {
                LogSkippedSymbolWithRsiAboveCandidate(TypeName, item.Symbol.Name, _options.Rsi.Periods, rsi, lowRsi, elected.Symbol.Name);
                continue;
            }

            // if we got here then we have a new candidate
            elected = item;
            highRelValue = stats.RelativeValue;
            lowRsi = rsi;
            LogSelectedNewCandidateForTopUpBuy(TypeName, item.Symbol.Name, stats.RelativeValue, _options.Rsi.Periods, rsi);
        }

        if (elected is null)
        {
            return false;
        }

        LogElectedSymbolForTopUpBuy(TypeName, elected.Name, elected.Ticker.ClosePrice, elected.Symbol.QuoteAsset, highRelValue, lowRsi);
        return true;
    }

    private bool TryElectPanicSell(out SymbolData[] elected)
    {
        LogEvaluatingSymbolForStopLoss(TypeName);

        var buffer = ArrayPool<SymbolData>.Shared.Rent(Context.Data.Count);
        var count = 0;

        // evaluate symbols with at least two positions
        foreach (var item in Context.Data.Where(x => x.AutoPosition.Positions.Count >= 2))
        {
            // calculate the stats vs the current price
            var stats = item.AutoPosition.Positions.GetStats(item.Ticker.ClosePrice);

            // only look at symbols with enough to sell
            if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity || stats.TotalCost < item.Symbol.Filters.MinNotional.MinNotional)
            {
                continue;
            }

            // flag symbols with relative value lower than minimum threshold
            if (stats.RelativeValue <= _options.RelativeValueForPanicSell)
            {
                buffer[count++] = item;
                LogElectedSymbolForStopLoss(TypeName, item.Symbol.Name, stats.RelativePnL);
            }
        }

        if (count > 0)
        {
            elected = buffer[0..count];
            ArrayPool<SymbolData>.Shared.Return(buffer);
            return true;
        }

        elected = Array.Empty<SymbolData>();
        ArrayPool<SymbolData>.Shared.Return(buffer);
        return false;
    }

    private IAlgoCommand CreateBuy(SymbolData item)
    {
        var total = item.Spot.QuoteAsset.Free
            + (_options.UseSavings ? item.Savings.QuoteAsset.FreeAmount : 0)
            + (_options.UseSwapPools ? item.SwapPools.QuoteAsset.Total : 0);

        total *= _options.BuyQuoteBalanceFraction;

        total = total.AdjustTotalUpToMinNotional(item.Symbol);

        if (_options.MaxNotional.HasValue)
        {
            total = Math.Max(total, _options.MaxNotional.Value);
        }

        var quantity = total / item.Ticker.ClosePrice;

        return MarketBuy(item.Symbol, quantity, _options.UseSavings, _options.UseSwapPools);
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} elected symbol {Symbol} for entry buy with ticker {Ticker:F8} {Quote} and RSI {Rsi:F8}")]
    private partial void LogElectedSymbolForEntryBuy(string type, string symbol, decimal ticker, string quote, decimal rsi);

    [LoggerMessage(1, LogLevel.Information, "{Type} elected symbol {Symbol} for top up buy with ticker {Ticker:F8} {Quote}, Relative Value = {RelValue:P8}, RSI {Rsi:F8}")]
    private partial void LogElectedSymbolForTopUpBuy(string type, string symbol, decimal ticker, string quote, decimal relValue, decimal rsi);

    [LoggerMessage(2, LogLevel.Information, "{Type} skipped symbol {Symbol} on cooldown until {Cooldown}")]
    private partial void LogSkippedSymbolOnCooldown(string type, string symbol, DateTime cooldown);

    [LoggerMessage(3, LogLevel.Information, "{Type} skipped symbol {Symbol} with Relative Value {RelValue:P2} lower than minimum {MinRelValue:P2} for top up buy")]
    private partial void LogSkippedSymbolWithLowRelativeValue(string type, string symbol, decimal relValue, decimal minRelValue);

    [LoggerMessage(4, LogLevel.Information, "{Type} skipped symbol {Symbol} with Relative Value {RelValue:P2} lower than candidate {HighRelValue:P2} from symbol {HighSymbol}")]
    private partial void LogSkippedSymbolWithLowerRelativeValueThanCandidate(string type, string symbol, decimal relValue, decimal highRelValue, string highSymbol);

    [LoggerMessage(5, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} above oversold of {Oversold:F8}")]
    private partial void LogSkippedSymbolWithRsiAboveOversold(string type, string symbol, int periods, decimal rsi, decimal oversold);

    [LoggerMessage(6, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} higher than candidate {LowRSI:F8} from symbol {LowSymbol}")]
    private partial void LogSkippedSymbolWithRsiAboveCandidate(string type, string symbol, int periods, decimal rsi, decimal lowRsi, string lowSymbol);

    [LoggerMessage(7, LogLevel.Information, "{Type} selected new candidate symbol for top up buy {Symbol} with Relative Value = {RelValue:F8} and RSI({Periods}) = {RSI:F8}")]
    private partial void LogSelectedNewCandidateForTopUpBuy(string type, string symbol, decimal relValue, decimal periods, decimal rsi);

    [LoggerMessage(8, LogLevel.Information, "{Type} evaluating symbols for a top up buy")]
    private partial void LogEvaluatingSymbolsForTopUpBuy(string type);

    [LoggerMessage(9, LogLevel.Information, "{Type} evaluating symbols for an entry buy")]
    private partial void LogEvaluatingSymbolsForEntryBuy(string type);

    [LoggerMessage(10, LogLevel.Information, "{Type} selected new candidate symbol for entry buy {Symbol} with RSI({Periods}) = {RSI:F8}")]
    private partial void LogSelectedNewCandidateForEntryBuy(string type, string symbol, decimal periods, decimal rsi);

    [LoggerMessage(11, LogLevel.Warning, "{Type} elected symbol {Symbol} for stop loss with PnL = {PnL:P8}")]
    private partial void LogElectedSymbolForStopLoss(string type, string symbol, decimal pnl);

    [LoggerMessage(12, LogLevel.Information, "{Type} skipped symbol {Symbol} with quantity {Quantity:F8} under min lot size {MinLotSize:F8}")]
    private partial void LogSkippedSymbolWithQuantityUnderMinLotSize(string type, string symbol, decimal quantity, decimal minLotSize);

    [LoggerMessage(13, LogLevel.Information, "{Type} skipped symbol {Symbol} with cost {Cost:F8} under min notional {MinNotional:F8}")]
    private partial void LogSkippedSymbolWithCostUnderMinNotional(string type, string symbol, decimal cost, decimal minNotional);

    [LoggerMessage(14, LogLevel.Information, "{Type} skipped symbol {Symbol} with quantity {Quantity:F8} above min {MinLotSize:F8} {Asset} and notional {Cost:F8} above min {MinNotional:F8} {Quote}")]
    private partial void LogSkippedSymbolWithQuantityAndNotionalAboveMin(string type, string symbol, decimal quantity, decimal minLotSize, string asset, decimal cost, decimal minNotional, string quote);

    [LoggerMessage(15, LogLevel.Information, "{Type} evaluating symbols for stop loss")]
    private partial void LogEvaluatingSymbolForStopLoss(string type);

    #endregion Logging
}