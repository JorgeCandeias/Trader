using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Portfolio;

public partial class PortfolioAlgo : Algo
{
    private readonly PortfolioAlgoOptions _options;
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;

    public PortfolioAlgo(IOptionsSnapshot<PortfolioAlgoOptions> options, ILogger<PortfolioAlgo> logger, ISystemClock clock)
    {
        _options = options.Get(Context.Name);
        _logger = logger;
        _clock = clock;
    }

    private const string TypeName = nameof(PortfolioAlgo);

    protected override IAlgoCommand OnExecute()
    {
        var now = _clock.UtcNow;

        var commands = new List<IAlgoCommand>();

        foreach (var item in Context.Data)
        {
            // skip symbol with invalid data
            if (!item.IsValid)
            {
                LogSkippedInvalidatedSymbol(TypeName, Context.Name, item.Symbol.Name);
                continue;
            }

            commands.Add(CreateTopUpBuy(item));
            commands.Add(CreateEntryBuy(item, now));
            commands.Add(CreateSell(item));
            commands.Add(CreateStopLoss(item));
        }

        ReportAggregateStats();

        return Sequence(commands);
    }

    private void ReportAggregateStats()
    {
        // report on total portfolio value for each quote
        foreach (var quote in Context.Data.GroupBy(x => x.Symbol.QuoteAsset))
        {
            // get stats for every sellable symbol
            var stats = quote.Select(x => (x.Symbol, Stats: x.AutoPosition.Positions.Reverse().EnumerateLots(x.Symbol.Filters.LotSize.StepSize).GetStats(x.Ticker.ClosePrice))).ToList();

            var (cost, pv, rpnl) = quote
                .Select(x =>
                (
                    Unrealized: x.AutoPosition.Positions.GetStats(x.Ticker.ClosePrice),
                    Realized: x.AutoPosition.ProfitEvents.Sum(x => x.Profit)
                ))
                .Aggregate(
                    (Cost: 0M, PV: 0M, RPNL: 0M),
                    (agg, item) => (agg.Cost + item.Unrealized.TotalCost, agg.PV + item.Unrealized.PresentValue, agg.RPNL + item.Realized));

            LogPortfolioInfo(
                TypeName,
                Context.Name,
                quote.Key,
                cost,
                pv,
                (pv - cost) / cost,
                pv - cost,
                rpnl,
                pv - cost + rpnl);

            // report on the absolute loser
            var absLoser = stats
                .OrderBy(x => x.Stats.AbsolutePnL)
                .FirstOrDefault();

            if (!IsNullOrEmpty(absLoser.Symbol.Name))
            {
                LogSymbolWithLowestAbsolutePnl(TypeName, Context.Name, absLoser.Symbol.Name, absLoser.Stats.AbsolutePnL);
            }

            // report on the relative loser
            var relLoser = stats
                .OrderBy(x => x.Stats.RelativePnL)
                .FirstOrDefault();

            if (!IsNullOrEmpty(relLoser.Symbol.Name))
            {
                LogSymbolWithLowestRelativePnl(TypeName, Context.Name, relLoser.Symbol.Name, relLoser.Stats.RelativePnL);
            }

            // report on the absolute winner
            var absWinner = stats
                .OrderByDescending(x => x.Stats.AbsolutePnL)
                .FirstOrDefault();

            if (!IsNullOrEmpty(absWinner.Symbol.Name))
            {
                LogSymbolWithHighestAbsolutePnl(TypeName, Context.Name, absWinner.Symbol.Name, absWinner.Stats.AbsolutePnL);
            }

            // report on the relative loser
            var relWinner = stats
                .OrderByDescending(x => x.Stats.RelativePnL)
                .FirstOrDefault();

            if (!IsNullOrEmpty(relWinner.Symbol.Name))
            {
                LogSymbolWithHighestRelativePnl(TypeName, Context.Name, relWinner.Symbol.Name, relWinner.Stats.RelativePnL);
            }
        }
    }

    private IAlgoCommand CreateStopLoss(SymbolData item)
    {
        if (!_options.StopLossEnabled)
        {
            return Noop();
        }

        // skip symbols to never sell
        if (_options.NeverSellSymbols.Contains(item.Symbol.Name))
        {
            LogStopLossSkippedSymbolOnNeverSellSet(TypeName, Context.Name, item.Symbol.Name);
            return Noop();
        }

        // skip symbol with open market orders
        if (item.Orders.Open.Any(x => x.Type == OrderType.Market))
        {
            LogStopLossSkippedSymbolWithOpenMarketOrders(TypeName, Context.Name, item.Symbol.Name, item.Orders.Open.Where(x => x.Type == OrderType.Market));
            return Noop();
        }

        // calculate the stats of the sellable lots vs the current price
        var sellable = item.AutoPosition.Positions.Reverse().EnumerateLots(item.Symbol.Filters.LotSize.StepSize).ToList();
        var stats = sellable.GetStats(item.Ticker.ClosePrice);

        // skip symbols with not enough quantity to sell
        if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
        {
            LogStopLossSkippedSymbolWithQuantityUnderMinLotSize(TypeName, Context.Name, item.Symbol.Name, stats.TotalQuantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
            return Noop();
        }

        // skip symbols with not enough notional to sell
        if (stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional)
        {
            LogStopLossSkippedSymbolWithNotionalUnderMinNotional(TypeName, Context.Name, item.Symbol.Name, stats.PresentValue, item.Symbol.Filters.MinNotional.MinNotional);
            return Noop();
        }

        // if we got here its safe to calculate a stop loss
        var stopPrice = sellable[0].AvgPrice * (1 - _options.StopLossRateFromLastPosition);
        stopPrice = stopPrice.AdjustPriceDownToTickSize(item.Symbol);

        // see if we hit the stop loss
        if (item.Ticker.ClosePrice > stopPrice)
        {
            return Noop();
        }

        // attempt to find the last positions that can be sold together at minimum profit on the current ticker
        var quantity = 0M;
        var cost = 0M;
        var pv = 0M;
        var minRelativeProfit = 1 + _options.MinStopLossProfitRate;
        foreach (var lot in sellable)
        {
            quantity += lot.Quantity;
            cost += lot.Quantity * lot.AvgPrice;
            pv += lot.Quantity * item.Ticker.ClosePrice;

            // see if we found a sellable combo
            if (pv > cost && pv >= item.Symbol.Filters.MinNotional.MinNotional && pv / cost >= minRelativeProfit)
            {
                LogStopLossElectedSymbol(TypeName, Context.Name, item.Symbol.Name, stats.RelativePnL);
                return MarketSell(item.Symbol, quantity, _options.UseSavings, _options.UseSwapPools);
            }
        }

        // if we got here its too late for the preemtive stop loss
        return Noop();
    }

    private IAlgoCommand CreateSell(SymbolData item)
    {
        if (!_options.SellingEnabled)
        {
            return Noop();
        }

        // skip symbols to never sell
        if (_options.NeverSellSymbols.Contains(item.Symbol.Name))
        {
            LogSellSkippedSymbolOnNeverSellSet(TypeName, Context.Name, item.Symbol.Name);
            return Noop();
        }

        // calculate the stats of the sellable lots vs the current price
        var stats = item.AutoPosition.Positions.Reverse().EnumerateLots(item.Symbol.Filters.LotSize.StepSize).GetStats(item.Ticker.ClosePrice);

        // skip symbols with not enough quantity to sell
        if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
        {
            LogSellSkippedSymbolWithQuantityUnderMinLotSize(TypeName, Context.Name, item.Symbol.Name, stats.TotalQuantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
            return Noop();
        }

        // skip symbols with not enough notional to sell
        if (stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional)
        {
            LogSellSkippedSymbolWithNotionalUnderMinNotional(TypeName, Context.Name, item.Symbol.Name, stats.PresentValue, item.Symbol.Filters.MinNotional.MinNotional);
            return Noop();
        }

        // skip symbols with not enough sell rsi
        var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Sell.Periods);
        if (rsi < _options.Rsi.Sell.Overbought)
        {
            LogSellSkippedSymbolWithLowSellRsi(TypeName, Context.Name, item.Symbol.Name, _options.Rsi.Sell.Periods, rsi, _options.Rsi.Sell.Overbought);
            return Noop();
        }

        LogSellElectedSymbol(TypeName, Context.Name, item.Symbol.Name, _options.Rsi.Sell.Periods, rsi);
        return AveragingSell(item.Symbol, _options.MinSellRate, _options.UseSavings, _options.UseSwapPools);
    }

    private IAlgoCommand CreateEntryBuy(SymbolData item, DateTime now)
    {
        // evaluate pnl
        var stats = item.AutoPosition.Positions.GetStats(item.Ticker.ClosePrice);

        // only look at symbols under the min lot size or min notional
        if (!(stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity || stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional))
        {
            LogEntryBuySkippedSymbolWithQuantityAndNotionalAboveMin(TypeName, Context.Name, item.Symbol.Name, stats.TotalQuantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity, stats.TotalCost, item.Symbol.Filters.MinNotional.MinNotional, item.Symbol.QuoteAsset);
            return Noop();
        }

        // skip symbols on cooldown
        if (item.AutoPosition.Positions.Count > 0)
        {
            var cooldown = item.AutoPosition.Positions.Last.Time.Add(_options.Cooldown);
            if (cooldown > now)
            {
                LogEntryBuySkippedSymbolOnCooldown(TypeName, Context.Name, item.Symbol.Name, cooldown);
                return Noop();
            }
        }

        // skip symbol with rsi above oversold
        var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Buy.Periods);
        if (rsi > _options.Rsi.Buy.Oversold)
        {
            LogEntryBuySkippedSymbolWithRsiAboveOversold(TypeName, Context.Name, item.Symbol.Name, _options.Rsi.Buy.Periods, rsi, _options.Rsi.Buy.Oversold);
            return Noop();
        }

        LogEntryBuyElectedSymbol(TypeName, Context.Name, item.Symbol.Name, item.Ticker.ClosePrice, item.Symbol.QuoteAsset, rsi);

        // identify the entry price
        var price = item.Klines.PriceForRsi(x => x.ClosePrice, _options.Rsi.Buy.Periods, _options.Rsi.Buy.Oversold, _options.Rsi.Buy.Precision);
        price = price.AdjustPriceUpToTickSize(item.Symbol);

        // identify the appropriate buy quantity for this price
        var quantity = CalculateBuyQuantity(item, price);

        // create the limit order
        return EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, null, price, null, _options.UseSavings, _options.UseSwapPools);
    }

    private IAlgoCommand CreateTopUpBuy(SymbolData item)
    {
        // evaluate pnl
        var stats = item.AutoPosition.Positions.GetStats(item.Ticker.ClosePrice);

        // skip symbols under the min lot size - leftovers are handled elsewhere
        if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
        {
            LogTopUpSkippedSymbolWithQuantityUnderMinLotSize(TypeName, Context.Name, item.Symbol.Name, stats.TotalQuantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
            return Noop();
        }

        // skip symbols under the min notional - leftovers are handled elsewhere
        if (stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional)
        {
            LogTopUpSkippedSymbolWithNotionalUnderMinNotional(TypeName, Context.Name, item.Symbol.Name, stats.PresentValue, item.Symbol.Filters.MinNotional.MinNotional);
            return Noop();
        }

        // skip symbols below min required for top up
        var price = item.AutoPosition.Positions.Last.Price * (1 + _options.MinChangeFromLastPositionPriceRequiredForTopUpBuy);
        price = price.AdjustPriceUpToTickSize(item.Symbol);

        if (item.Ticker.ClosePrice < price)
        {
            LogTopUpSkippedSymbolWithPriceNotHighEnough(TypeName, Context.Name, item.Symbol.Name, item.Ticker.ClosePrice, price, _options.MinChangeFromLastPositionPriceRequiredForTopUpBuy, item.AutoPosition.Positions.Last.Price);
            return Noop();
        }

        // if we got here then we have a top up
        LogTopUpElectedSymbol(TypeName, Context.Name, item.Name, item.Ticker.ClosePrice, item.Symbol.QuoteAsset);

        // raise to the current ticker if needed
        price = Math.Max(item.Ticker.ClosePrice, price);

        // identify the appropriate buy quantity for this price
        var quantity = CalculateBuyQuantity(item, price);

        // skip if there is already an open order at an equal or higher ticker to avoid order twitching
        if (item.Orders.Open.Any(x => x.Side == OrderSide.Buy && x.Type == OrderType.Limit && x.OriginalQuantity == quantity && x.Price >= price))
        {
            return Noop();
        }

        // create the limit order
        return EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, null, price, null, _options.UseSavings, _options.UseSwapPools);
    }

    private decimal CalculateBuyQuantity(SymbolData item, decimal price)
    {
        // calculate the notional to use for buying
        var notional = item.Spot.QuoteAsset.Free
            + (_options.UseSavings ? item.Savings.QuoteAsset.FreeAmount : 0)
            + (_options.UseSwapPools ? item.SwapPools.QuoteAsset.Total : 0);

        notional *= _options.BuyQuoteBalanceFraction;

        // raise to a valid number
        notional = notional.AdjustTotalUpToMinNotional(item.Symbol);
        notional = notional.AdjustPriceUpToTickSize(item.Symbol);

        // pad the order with the fee
        notional *= (1 + _options.FeeRate);

        // raise again to a valid number
        notional = notional.AdjustPriceUpToTickSize(item.Symbol);

        // calculate the quantity for the limit order
        var quantity = notional / price;

        // raise the quantity to a valid number
        quantity = quantity.AdjustQuantityUpToMinLotSizeQuantity(item.Symbol);
        quantity = quantity.AdjustQuantityUpToLotStepSize(item.Symbol);

        return quantity;
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} {Name} elected symbol {Symbol} for entry buy with ticker {Ticker:F8} {Quote} and RSI {Rsi:F8}")]
    private partial void LogEntryBuyElectedSymbol(string type, string name, string symbol, decimal ticker, string quote, decimal rsi);

    [LoggerMessage(1, LogLevel.Information, "{Type} {Name} top up elected symbol {Symbol} with ticker {Ticker:F8} {Quote}")]
    private partial void LogTopUpElectedSymbol(string type, string name, string symbol, decimal ticker, string quote);

    [LoggerMessage(2, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} on cooldown until {Cooldown}")]
    private partial void LogTopUpSkippedSymbolOnCooldown(string type, string name, string symbol, DateTime cooldown);

    [LoggerMessage(3, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} with Ticker {Ticker:F8} lower than minimum {MinPrice:F8} as {Rate:P2} of last position price {LastPrice:F8}")]
    private partial void LogTopUpSkippedSymbolWithPriceNotHighEnough(string type, string name, string symbol, decimal ticker, decimal minPrice, decimal rate, decimal lastPrice);

    [LoggerMessage(4, LogLevel.Information, "{Type} skipped symbol {Symbol} with Relative Value {RelValue:P2} lower than candidate {HighRelValue:P2} from symbol {HighSymbol}")]
    private partial void LogSkippedSymbolWithLowerRelativeValueThanCandidate(string type, string symbol, decimal relValue, decimal highRelValue, string highSymbol);

    [LoggerMessage(5, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} above oversold of {Oversold:F8}")]
    private partial void LogSkippedSymbolWithRsiAboveOversold(string type, string symbol, int periods, decimal rsi, decimal oversold);

    [LoggerMessage(6, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} higher than candidate {LowRSI:F8} from symbol {LowSymbol}")]
    private partial void LogSkippedSymbolWithRsiAboveCandidate(string type, string symbol, int periods, decimal rsi, decimal lowRsi, string lowSymbol);

    [LoggerMessage(7, LogLevel.Information, "{Type} selected new candidate symbol for top up buy {Symbol} with Ticker = {Ticker:F8} {Quote}")]
    private partial void LogSelectedNewCandidateForTopUpBuy(string type, string symbol, decimal ticker, string quote);

    [LoggerMessage(9, LogLevel.Information, "{Type} evaluating symbols for an entry buy")]
    private partial void LogEvaluatingSymbolsForEntryBuy(string type);

    [LoggerMessage(10, LogLevel.Information, "{Type} selected new candidate symbol for entry buy {Symbol} with RSI({Periods}) = {RSI:F8}")]
    private partial void LogSelectedNewCandidateForEntryBuy(string type, string symbol, decimal periods, decimal rsi);

    [LoggerMessage(11, LogLevel.Warning, "{Type} {Name} stop loss elected symbol {Symbol} with PnL = {PnL:P8}")]
    private partial void LogStopLossElectedSymbol(string type, string name, string symbol, decimal pnl);

    [LoggerMessage(12, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} with quantity {Quantity:F8} {Asset} under min lot size {MinLotSize:F8}")]
    private partial void LogTopUpSkippedSymbolWithQuantityUnderMinLotSize(string type, string name, string symbol, decimal quantity, string asset, decimal minLotSize);

    [LoggerMessage(13, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} with present notional {Notional:F8} under min notional {MinNotional:F8}")]
    private partial void LogTopUpSkippedSymbolWithNotionalUnderMinNotional(string type, string name, string symbol, decimal notional, decimal minNotional);

    [LoggerMessage(14, LogLevel.Information, "{Type} skipped symbol {Symbol} with quantity {Quantity:F8} {Asset} above min {MinLotSize:F8} {Asset} and notional {Cost:F8} above min {MinNotional:F8} {Quote}")]
    private partial void LogSkippedSymbolWithQuantityAndNotionalAboveMin(string type, string symbol, decimal quantity, string asset, decimal minLotSize, decimal cost, decimal minNotional, string quote);

    [LoggerMessage(15, LogLevel.Information, "{Type} evaluating symbols for stop loss")]
    private partial void LogEvaluatingSymbolsForStopLoss(string type);

    [LoggerMessage(16, LogLevel.Information, "{Type} {Name} sell skipped symbol {Symbol} on the never sell set")]
    private partial void LogSellSkippedSymbolOnNeverSellSet(string type, string name, string symbol);

    [LoggerMessage(17, LogLevel.Information, "{Type} evaluating symbols for pump sell")]
    private partial void LogEvaluatingSymbolsForPumpSell(string type);

    [LoggerMessage(18, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} lower than sell RSI({Periods}) {SellRSI:F8}")]
    private partial void LogSkippedSymbolWithLowSellRsi(string type, string symbol, int periods, decimal rsi, decimal sellRsi);

    [LoggerMessage(19, LogLevel.Information, "{Type} {Name} elected symbol {Symbol} with RSI({Periods}) {RSI:F8} for pump sell")]
    private partial void LogSellElectedSymbol(string type, string name, string symbol, int periods, decimal rsi);

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

    [LoggerMessage(28, LogLevel.Information, "{Type} {Name} reports {Quote} portfolio info (U-Cost: {UCost:F8}, U-PV: {UPV:F8}: U-RPnL: {URPNL:P2}, U-AbsPnL: {UAPNL:F8}, R-AbsPnL: {RAPNL:F8}, T-AbsPnL:{TAPNL:F8})")]
    private partial void LogPortfolioInfo(string type, string name, string quote, decimal ucost, decimal upv, decimal urpnl, decimal uapnl, decimal rapnl, decimal tapnl);

    [LoggerMessage(29, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with lowest unrealized absolute pnl {unrealizedAbsolutePnl:F8}")]
    private partial void LogSymbolWithLowestAbsolutePnl(string type, string name, string symbol, decimal unrealizedAbsolutePnl);

    [LoggerMessage(30, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with lowest unrealized relative pnl {unrealizedRelativePnl:P2}")]
    private partial void LogSymbolWithLowestRelativePnl(string type, string name, string symbol, decimal unrealizedRelativePnl);

    [LoggerMessage(29, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with highest unrealized absolute pnl {unrealizedAbsolutePnl:F8}")]
    private partial void LogSymbolWithHighestAbsolutePnl(string type, string name, string symbol, decimal unrealizedAbsolutePnl);

    [LoggerMessage(30, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with highest unrealized relative pnl {unrealizedRelativePnl:P2}")]
    private partial void LogSymbolWithHighestRelativePnl(string type, string name, string symbol, decimal unrealizedRelativePnl);

    [LoggerMessage(31, LogLevel.Information, "{Type} {Name} stop loss skipped symbol {Symbol} on the never sell set")]
    private partial void LogStopLossSkippedSymbolOnNeverSellSet(string type, string name, string symbol);

    [LoggerMessage(32, LogLevel.Warning, "{Type} {Name} stop loss skipped symbol {Symbol} with open market orders {Orders}")]
    private partial void LogStopLossSkippedSymbolWithOpenMarketOrders(string type, string name, string symbol, IEnumerable<OrderQueryResult> orders);

    [LoggerMessage(33, LogLevel.Information, "{Type} {Name} stop loss skipped symbol {Symbol} with quantity {Quantity:F8} {Asset} under min lot size {MinLotSize:F8}")]
    private partial void LogStopLossSkippedSymbolWithQuantityUnderMinLotSize(string type, string name, string symbol, decimal quantity, string asset, decimal minLotSize);

    [LoggerMessage(34, LogLevel.Information, "{Type} {Name} stop loss skipped symbol {Symbol} with present notional {Notional:F8} under min notional {MinNotional:F8}")]
    private partial void LogStopLossSkippedSymbolWithNotionalUnderMinNotional(string type, string name, string symbol, decimal notional, decimal minNotional);

    [LoggerMessage(35, LogLevel.Information, "{Type} {Name} sell skipped symbol {Symbol} with quantity {Quantity:F8} {Asset} under min lot size {MinLotSize:F8}")]
    private partial void LogSellSkippedSymbolWithQuantityUnderMinLotSize(string type, string name, string symbol, decimal quantity, string asset, decimal minLotSize);

    [LoggerMessage(36, LogLevel.Information, "{Type} {Name} sell skipped symbol {Symbol} with present notional {Notional:F8} under min notional {MinNotional:F8}")]
    private partial void LogSellSkippedSymbolWithNotionalUnderMinNotional(string type, string name, string symbol, decimal notional, decimal minNotional);

    [LoggerMessage(37, LogLevel.Information, "{Type} {Name} sell symbol {Symbol} with RSI({Periods}) {RSI:F8} lower than sell RSI({Periods}) {SellRSI:F8}")]
    private partial void LogSellSkippedSymbolWithLowSellRsi(string type, string name, string symbol, int periods, decimal rsi, decimal sellRsi);

    [LoggerMessage(38, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} with quantity {Quantity:F8} {Asset} above min {MinLotSize:F8} {Asset} and notional {Cost:F8} above min {MinNotional:F8} {Quote}")]
    private partial void LogEntryBuySkippedSymbolWithQuantityAndNotionalAboveMin(string type, string name, string symbol, decimal quantity, string asset, decimal minLotSize, decimal cost, decimal minNotional, string quote);

    [LoggerMessage(39, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} on cooldown until {Cooldown}")]
    private partial void LogEntryBuySkippedSymbolOnCooldown(string type, string name, string symbol, DateTime cooldown);

    [LoggerMessage(40, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} above oversold of {Oversold:F8}")]
    private partial void LogEntryBuySkippedSymbolWithRsiAboveOversold(string type, string name, string symbol, int periods, decimal rsi, decimal oversold);

    #endregion Logging
}