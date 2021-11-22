using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
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

        if (TryElectPumpSell(out var pumped))
        {
            return Sequence(pumped.Select(x => AveragingSell(x.Symbol, _options.MinSellRate, _options.UseSavings, _options.UseSwapPools)));
        }

        if (TryElectStopLoss(out var losers))
        {
            return Sequence(losers.Select(x => AveragingSell(x.Symbol, _options.MinStopLossProfitRate, _options.UseSavings, _options.UseSwapPools)));
        }

        return Noop();
    }

    private bool TryElectStopLoss(out SymbolData[] elected)
    {
        if (!_options.StopLossEnabled)
        {
            LogStopLossDisabled(TypeName);
            elected = Array.Empty<SymbolData>();
            return false;
        }

        LogEvaluatingSymbolsForStopLoss(TypeName);

        var buffer = ArrayPool<SymbolData>.Shared.Rent(Context.Data.Count);
        var count = 0;

        foreach (var item in Context.Data)
        {
            // skip symbol with invalid data
            if (!item.IsValid)
            {
                LogSkippedInvalidatedSymbol(TypeName, Context.Name, item.Symbol.Name);
                continue;
            }

            // skip symbols to never sell
            if (_options.NeverSellSymbols.Contains(item.Symbol.Name))
            {
                LogSkippedSymbolOnNeverSellSet(TypeName, item.Symbol.Name);
                continue;
            }

            // skip symbol with open market orders
            if (item.Orders.Open.Any(x => x.Type == OrderType.Market))
            {
                LogSkippedSymbolWithOpenMarketOrders(TypeName, Context.Name, item.Symbol.Name, item.Orders.Open.Where(x => x.Type == OrderType.Market));
                continue;
            }

            // calculate the stats vs the current price
            var stats = item.AutoPosition.Positions.GetStats(item.Ticker.ClosePrice);

            // skip symbols with not enough quantity to sell
            if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                LogSkippedSymbolWithQuantityUnderMinLotSize(TypeName, item.Symbol.Name, stats.TotalQuantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
                continue;
            }

            // skip symbols with not enough notional to sell
            if (stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional)
            {
                LogSkippedSymbolWithNotionalUnderMinNotional(TypeName, item.Symbol.Name, stats.PresentValue, item.Symbol.Filters.MinNotional.MinNotional);
                continue;
            }

            // skip symbols with not enough loss from last position
            var stopLossPrice = item.AutoPosition.Positions.Last.Price * _options.StopLossRateFromLastPosition;
            if (item.Ticker.ClosePrice > stopLossPrice)
            {
                LogSkippedSymbolWithCurrentPriceNotBelowStopLossPrice(TypeName, item.Symbol.Name, item.Ticker.ClosePrice, item.Symbol.QuoteAsset, stopLossPrice, _options.StopLossRateFromLastPosition, item.AutoPosition.Positions.Last.Price);
                continue;
            }

            // skip symbols with positions that cannot be sold at the minimum profit rate
            var minSellPrice = stats.AvgPrice * _options.MinStopLossProfitRate;
            if (item.Ticker.ClosePrice < minSellPrice)
            {
                LogSkippedSymbolWithCurrentPriceLowerThanMinSellPrice(TypeName, item.Symbol.Name, item.Ticker.ClosePrice, item.Symbol.QuoteAsset, minSellPrice, _options.MinStopLossProfitRate, stats.AvgPrice);
                continue;
            }

            // if we got here then we have a candidate for stop loss
            buffer[count++] = item;
            LogElectedSymbolForStopLoss(TypeName, item.Symbol.Name, stats.RelativePnL);
        }

        var success = count > 0;
        elected = success ? buffer[0..count] : Array.Empty<SymbolData>();
        return success;
    }

    private bool TryElectPumpSell(out SymbolData[] elected)
    {
        if (!_options.SellingEnabled)
        {
            LogSellingDisabled(TypeName, Context.Name);

            elected = Array.Empty<SymbolData>();
            return false;
        }

        LogEvaluatingSymbolsForPumpSell(TypeName);

        var buffer = ArrayPool<SymbolData>.Shared.Rent(Context.Data.Count);
        var count = 0;

        foreach (var item in Context.Data)
        {
            // skip symbol with invalid data
            if (!item.IsValid)
            {
                LogSkippedInvalidatedSymbol(TypeName, Context.Name, item.Symbol.Name);
                continue;
            }

            // skip symbols to never sell
            if (_options.NeverSellSymbols.Contains(item.Symbol.Name))
            {
                LogSkippedSymbolOnNeverSellSet(TypeName, item.Symbol.Name);
                continue;
            }

            // skip symbol with open market orders
            if (item.Orders.Open.Any(x => x.Type == OrderType.Market))
            {
                LogSkippedSymbolWithOpenMarketOrders(TypeName, Context.Name, item.Symbol.Name, item.Orders.Open.Where(x => x.Type == OrderType.Market));
                continue;
            }

            // calculate the stats vs the current price
            var stats = item.AutoPosition.Positions.GetStats(item.Ticker.ClosePrice);

            // skip symbols with not enough quantity to sell
            if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                LogSkippedSymbolWithQuantityUnderMinLotSize(TypeName, item.Symbol.Name, stats.TotalQuantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
                continue;
            }

            // skip symbols with not enough notional to sell
            if (stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional)
            {
                LogSkippedSymbolWithNotionalUnderMinNotional(TypeName, item.Symbol.Name, stats.PresentValue, item.Symbol.Filters.MinNotional.MinNotional);
                continue;
            }

            // skip symbols with not enough sell rsi
            var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Sell.Periods);
            if (rsi < _options.Rsi.Sell.Overbought)
            {
                LogSkippedSymbolWithLowSellRsi(TypeName, item.Symbol.Name, _options.Rsi.Sell.Periods, rsi, _options.Rsi.Sell.Overbought);
                continue;
            }

            // if we got here then we have a pumped symbol
            buffer[count++] = item;
            LogElectedSymbolForPumpSell(TypeName, item.Symbol.Name, _options.Rsi.Sell.Periods, rsi);
        }

        elected = buffer[0..count];
        ArrayPool<SymbolData>.Shared.Return(buffer);
        return count > 0;
    }

    private bool TryElectEntryBuy(out SymbolData elected)
    {
        LogEvaluatingSymbolsForEntryBuy(TypeName);

        elected = null!;
        var lastRsi = decimal.MaxValue;
        var now = _clock.UtcNow;

        foreach (var item in Context.Data)
        {
            // skip symbol with invalid data
            if (!item.IsValid)
            {
                LogSkippedInvalidatedSymbol(TypeName, Context.Name, item.Symbol.Name);
                continue;
            }

            // skip symbol with open market orders
            if (item.Orders.Open.Any(x => x.Type == OrderType.Market))
            {
                LogSkippedSymbolWithOpenMarketOrders(TypeName, Context.Name, item.Symbol.Name, item.Orders.Open.Where(x => x.Type == OrderType.Market));
                continue;
            }

            // evaluate pnl
            var stats = item.AutoPosition.Positions.GetStats(item.Ticker.ClosePrice);

            // only look at symbols under the min lot size or min notional
            if (!(stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity || stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional))
            {
                LogSkippedSymbolWithQuantityAndNotionalAboveMin(TypeName, item.Symbol.Name, stats.TotalQuantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity, stats.TotalCost, item.Symbol.Filters.MinNotional.MinNotional, item.Symbol.QuoteAsset);
                continue;
            }

            // skip symbols on cooldown
            if (item.AutoPosition.Positions.Count > 0)
            {
                var cooldown = item.AutoPosition.Positions.Last.Time.Add(_options.Cooldown);
                if (cooldown > now)
                {
                    LogSkippedSymbolOnCooldown(TypeName, item.Symbol.Name, cooldown);
                    continue;
                }
            }

            // evaluate the rsi for the symbol
            var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Buy.Periods);

            // skip symbol with rsi above oversold
            if (rsi > _options.Rsi.Buy.Oversold)
            {
                LogSkippedSymbolWithRsiAboveOversold(TypeName, item.Symbol.Name, _options.Rsi.Buy.Periods, rsi, _options.Rsi.Buy.Oversold);
                continue;
            }

            // skip symbol with rsi above current candidate
            if (elected is not null && rsi > lastRsi)
            {
                LogSkippedSymbolWithRsiAboveCandidate(TypeName, item.Symbol.Name, _options.Rsi.Buy.Periods, rsi, lastRsi, elected.Symbol.Name);
                continue;
            }

            // if we got here then we have a new candidate
            elected = item;
            lastRsi = rsi;
            LogSelectedNewCandidateForEntryBuy(TypeName, item.Symbol.Name, _options.Rsi.Buy.Periods, rsi);
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
        var now = _clock.UtcNow;

        // evaluate symbols with at least one position
        foreach (var item in Context.Data)
        {
            // skip symbol with invalid data
            if (!item.IsValid)
            {
                LogSkippedInvalidatedSymbol(TypeName, Context.Name, item.Symbol.Name);
                continue;
            }

            // skip symbol with open market orders
            if (item.Orders.Open.Any(x => x.Type == OrderType.Market))
            {
                LogSkippedSymbolWithOpenMarketOrders(TypeName, Context.Name, item.Symbol.Name, item.Orders.Open.Where(x => x.Type == OrderType.Market));
                continue;
            }

            // evaluate pnl
            var stats = item.AutoPosition.Positions.GetStats(item.Ticker.ClosePrice);

            // skip symbols under the min lot size - leftovers are handled by the entry signal
            if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                LogSkippedSymbolWithQuantityUnderMinLotSize(TypeName, item.Symbol.Name, stats.TotalQuantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
                continue;
            }

            // skip symbols under the min notional - leftovers are handled by the entry signal
            if (stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional)
            {
                LogSkippedSymbolWithNotionalUnderMinNotional(TypeName, item.Symbol.Name, stats.PresentValue, item.Symbol.Filters.MinNotional.MinNotional);
                continue;
            }

            // skip symbols on cooldown
            var cooldown = item.AutoPosition.Positions.Last.Time.Add(_options.Cooldown);
            if (cooldown > now)
            {
                LogSkippedSymbolOnCooldown(TypeName, item.Symbol.Name, cooldown);
                continue;
            }

            // skip symbols with negative relative value
            if (stats.RelativePnL < 0)
            {
                LogSkippedSymbolWithNegativeRelativePnL(TypeName, item.Symbol.Name, stats.RelativePnL);
                continue;
            }

            // skip symbols with rsi overbought
            var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Buy.Periods);
            if (rsi >= _options.Rsi.Buy.Overbought)
            {
                LogSkippedSymbolWithRsiAboveOverbought(TypeName, item.Symbol.Name, _options.Rsi.Buy.Periods, rsi, _options.Rsi.Buy.Overbought);
                continue;
            }

            // skip symbols below min required for top up
            var minPrice = item.AutoPosition.Positions.Last.Price * _options.MinChangeFromLastPositionPriceRequiredForTopUpBuy;
            if (item.Ticker.ClosePrice < minPrice)
            {
                LogSkippedSymbolWithPriceNotHighEnough(TypeName, item.Symbol.Name, item.Ticker.ClosePrice, minPrice, _options.MinChangeFromLastPositionPriceRequiredForTopUpBuy, item.AutoPosition.Positions.Last.Price);
                continue;
            }

            // skip symbols below the highest candidate yet
            if (elected is not null && stats.RelativeValue <= highRelValue)
            {
                LogSkippedSymbolWithLowerRelativeValueThanCandidate(TypeName, item.Symbol.Name, stats.RelativeValue, highRelValue, elected.Symbol.Name);
                continue;
            }

            // if we got here then we have a new candidate
            elected = item;
            highRelValue = stats.RelativeValue;
            LogSelectedNewCandidateForTopUpBuy(TypeName, item.Symbol.Name, item.Ticker.ClosePrice, item.Symbol.QuoteAsset);
        }

        if (elected is null)
        {
            return false;
        }

        LogElectedSymbolForTopUpBuy(TypeName, elected.Name, elected.Ticker.ClosePrice, elected.Symbol.QuoteAsset);
        return true;
    }

    private IAlgoCommand CreateBuy(SymbolData item)
    {
        // calculate the notional to use for buying
        var total = item.Spot.QuoteAsset.Free
            + (_options.UseSavings ? item.Savings.QuoteAsset.FreeAmount : 0)
            + (_options.UseSwapPools ? item.SwapPools.QuoteAsset.Total : 0);

        total *= _options.BuyQuoteBalanceFraction;

        total = total.AdjustTotalUpToMinNotional(item.Symbol);

        if (_options.MaxNotional.HasValue)
        {
            total = Math.Max(total, _options.MaxNotional.Value);
        }

        // calculate the quantity from the notional
        var quantity = total / item.Ticker.ClosePrice;

        // adjust the quantity up to the min lot size
        quantity = quantity.AdjustQuantityUpToMinLotSizeQuantity(item.Symbol);

        // pad the order with the fee
        quantity *= (1 + _options.FeeRate);

        // adjust again to the step size
        quantity = quantity.AdjustQuantityUpToLotStepSize(item.Symbol);

        return MarketBuy(item.Symbol, quantity, _options.UseSavings, _options.UseSwapPools);
    }

    #region Logging

    // add the algo name to all logs

    [LoggerMessage(0, LogLevel.Information, "{Type} elected symbol {Symbol} for entry buy with ticker {Ticker:F8} {Quote} and RSI {Rsi:F8}")]
    private partial void LogElectedSymbolForEntryBuy(string type, string symbol, decimal ticker, string quote, decimal rsi);

    [LoggerMessage(1, LogLevel.Information, "{Type} elected symbol {Symbol} for top up buy with ticker {Ticker:F8} {Quote}")]
    private partial void LogElectedSymbolForTopUpBuy(string type, string symbol, decimal ticker, string quote);

    [LoggerMessage(2, LogLevel.Information, "{Type} skipped symbol {Symbol} on cooldown until {Cooldown}")]
    private partial void LogSkippedSymbolOnCooldown(string type, string symbol, DateTime cooldown);

    [LoggerMessage(3, LogLevel.Information, "{Type} skipped symbol {Symbol} with Ticker {Ticker:F8} lower than minimum {MinPrice:F8} as {Rate:P2} of last position price {LastPrice:F8}")]
    private partial void LogSkippedSymbolWithPriceNotHighEnough(string type, string symbol, decimal ticker, decimal minPrice, decimal rate, decimal lastPrice);

    [LoggerMessage(4, LogLevel.Information, "{Type} skipped symbol {Symbol} with Relative Value {RelValue:P2} lower than candidate {HighRelValue:P2} from symbol {HighSymbol}")]
    private partial void LogSkippedSymbolWithLowerRelativeValueThanCandidate(string type, string symbol, decimal relValue, decimal highRelValue, string highSymbol);

    [LoggerMessage(5, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} above oversold of {Oversold:F8}")]
    private partial void LogSkippedSymbolWithRsiAboveOversold(string type, string symbol, int periods, decimal rsi, decimal oversold);

    [LoggerMessage(6, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} higher than candidate {LowRSI:F8} from symbol {LowSymbol}")]
    private partial void LogSkippedSymbolWithRsiAboveCandidate(string type, string symbol, int periods, decimal rsi, decimal lowRsi, string lowSymbol);

    [LoggerMessage(7, LogLevel.Information, "{Type} selected new candidate symbol for top up buy {Symbol} with Ticker = {Ticker:F8} {Quote}")]
    private partial void LogSelectedNewCandidateForTopUpBuy(string type, string symbol, decimal ticker, string quote);

    [LoggerMessage(8, LogLevel.Information, "{Type} evaluating symbols for a top up buy")]
    private partial void LogEvaluatingSymbolsForTopUpBuy(string type);

    [LoggerMessage(9, LogLevel.Information, "{Type} evaluating symbols for an entry buy")]
    private partial void LogEvaluatingSymbolsForEntryBuy(string type);

    [LoggerMessage(10, LogLevel.Information, "{Type} selected new candidate symbol for entry buy {Symbol} with RSI({Periods}) = {RSI:F8}")]
    private partial void LogSelectedNewCandidateForEntryBuy(string type, string symbol, decimal periods, decimal rsi);

    [LoggerMessage(11, LogLevel.Warning, "{Type} elected symbol {Symbol} for stop loss with PnL = {PnL:P8}")]
    private partial void LogElectedSymbolForStopLoss(string type, string symbol, decimal pnl);

    [LoggerMessage(12, LogLevel.Information, "{Type} skipped symbol {Symbol} with quantity {Quantity:F8} {Asset} under min lot size {MinLotSize:F8}")]
    private partial void LogSkippedSymbolWithQuantityUnderMinLotSize(string type, string symbol, decimal quantity, string asset, decimal minLotSize);

    [LoggerMessage(13, LogLevel.Information, "{Type} skipped symbol {Symbol} with present notional {Notional:F8} under min notional {MinNotional:F8}")]
    private partial void LogSkippedSymbolWithNotionalUnderMinNotional(string type, string symbol, decimal notional, decimal minNotional);

    [LoggerMessage(14, LogLevel.Information, "{Type} skipped symbol {Symbol} with quantity {Quantity:F8} {Asset} above min {MinLotSize:F8} {Asset} and notional {Cost:F8} above min {MinNotional:F8} {Quote}")]
    private partial void LogSkippedSymbolWithQuantityAndNotionalAboveMin(string type, string symbol, decimal quantity, string asset, decimal minLotSize, decimal cost, decimal minNotional, string quote);

    [LoggerMessage(15, LogLevel.Information, "{Type} evaluating symbols for stop loss")]
    private partial void LogEvaluatingSymbolsForStopLoss(string type);

    [LoggerMessage(16, LogLevel.Information, "{Type} skipped symbol {Symbol} on the never sell set")]
    private partial void LogSkippedSymbolOnNeverSellSet(string type, string symbol);

    [LoggerMessage(17, LogLevel.Information, "{Type} evaluating symbols for pump sell")]
    private partial void LogEvaluatingSymbolsForPumpSell(string type);

    [LoggerMessage(18, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} lower than sell RSI({Periods}) {SellRSI:F8}")]
    private partial void LogSkippedSymbolWithLowSellRsi(string type, string symbol, int periods, decimal rsi, decimal sellRsi);

    [LoggerMessage(19, LogLevel.Information, "{Type} elected symbol {Symbol} with RSI({Periods}) {RSI:F8} for pump sell")]
    private partial void LogElectedSymbolForPumpSell(string type, string symbol, int periods, decimal rsi);

    [LoggerMessage(20, LogLevel.Information, "{Type} skipped symbol {Symbol} with negative relative value {PNL:P2}")]
    private partial void LogSkippedSymbolWithNegativeRelativePnL(string type, string symbol, decimal pnl);

    [LoggerMessage(21, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} above overbought RSI({Periods}) {Overbought:F8}")]
    private partial void LogSkippedSymbolWithRsiAboveOverbought(string type, string symbol, int periods, decimal rsi, decimal overbought);

    [LoggerMessage(22, LogLevel.Information, "{Type} skipped symbol {Symbol} with current price {Price:F8} {Quote} not below stop loss price of {StopLossPrice:F8} {Quote} ({StopLossRate:P2} of {LastPrice:F8} {Quote})")]
    private partial void LogSkippedSymbolWithCurrentPriceNotBelowStopLossPrice(string type, string symbol, decimal price, string quote, decimal stopLossPrice, decimal stopLossRate, decimal lastPrice);

    [LoggerMessage(23, LogLevel.Information, "{Type} skipped symbol {Symbol} with current price {Price:F8} {Quote} lower than min sell price {MinSellPrice:F8} {Quote} ({MinSellRate:P2} of average buy price {AvgPrice:F8} {Quote})")]
    private partial void LogSkippedSymbolWithCurrentPriceLowerThanMinSellPrice(string type, string symbol, decimal price, string quote, decimal minSellPrice, decimal minSellRate, decimal avgPrice);

    [LoggerMessage(24, LogLevel.Information, "{Type} reports stop loss is disabled")]
    private partial void LogStopLossDisabled(string type);

    [LoggerMessage(25, LogLevel.Information, "{Type} {Name} reports selling is disabled")]
    private partial void LogSellingDisabled(string type, string name);

    [LoggerMessage(26, LogLevel.Error, "{Type} {Name} skipped invalidated symbol {Symbol}")]
    private partial void LogSkippedInvalidatedSymbol(string type, string name, string symbol);

    [LoggerMessage(27, LogLevel.Warning, "{Type} {Name} skipped symbol {Symbol} with open market orders {Orders}")]
    private partial void LogSkippedSymbolWithOpenMarketOrders(string type, string name, string symbol, IEnumerable<OrderQueryResult> orders);

    #endregion Logging
}