using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Positions;
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
    private const string RecoverySellTag = "RecoverySell";
    private const string RecoveryBuyTag = "RecoveryBuy";

    protected override IAlgoCommand OnExecute()
    {
        var commands = new List<IAlgoCommand>(Context.Data.Count * 5);
        var statsLookup = new Dictionary<string, PositionStats>();

        foreach (var item in Context.Data)
        {
            // get sellable lots from the end
            var lots = item.AutoPosition.Positions.Reverse().EnumerateLots(item.Symbol.Filters.LotSize.StepSize).ToList();

            // get the stats for sellable lots
            var stats = lots.GetStats(item.Ticker.ClosePrice);

            // cache for the reporting method
            statsLookup[item.Symbol.Name] = stats;

            // skip symbol with invalid data
            if (!item.IsValid)
            {
                LogSkippedInvalidatedSymbol(TypeName, Context.Name, item.Symbol.Name);
                continue;
            }

            commands.Add(CreateSellOff(item, lots, stats));
            commands.Add(CreateRecoverySell(item, lots, stats));
            commands.Add(CreateRecoveryBuy(item, lots));
            commands.Add(CreateTopUpBuy(item, lots));
            commands.Add(CreateEntryBuy(item, lots));
        }

        ReportAggregateStats(statsLookup);

        return Sequence(commands);
    }

    private void ReportAggregateStats(IDictionary<string, PositionStats> statsLookup)
    {
        var reportable = Context.Data.Where(x => !_options.NeverSellSymbols.Contains(x.Symbol.Name));
        var grouped = reportable.GroupBy(x => x.Symbol.QuoteAsset);

        // report on total portfolio value for each quote
        foreach (var quote in grouped)
        {
            // get stats for every sellable symbol
            var stats = quote.Select(x => (x.Symbol, Stats: statsLookup[x.Symbol.Name]));

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

            // top 10 break even
            foreach (var item in stats
                .Where(x => x.Stats.TotalQuantity > 0)
                .Where(x => x.Stats.RelativePnL >= 0)
                .OrderBy(x => x.Stats.RelativePnL))
            {
                LogSymbolAtBreakEven(TypeName, Context.Name, item.Symbol.Name, item.Stats.RelativePnL, item.Stats.AbsolutePnL, item.Symbol.QuoteAsset, item.Stats.PresentValue);
            }
        }
    }

    private IAlgoCommand CreateSellOff(SymbolData item, IList<PositionLot> lots, PositionStats stats)
    {
        // there must be enough quantity to sell off right now
        if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
        {
            return Noop();
        }

        // there must be enough notional value to sell off right now
        if (stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional)
        {
            return Noop();
        }

        // the present pnl must be at or above the requirement
        if (stats.RelativeValue < _options.SellOffTriggerRate)
        {
            return Noop();
        }

        // the present rsi must be at or above the requirement
        var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Sell.Periods);
        if (rsi < _options.Rsi.Sell.Overbought)
        {
            return Noop();
        }

        // if all the stars align then dump the assets
        return MarketSell(item.Symbol, stats.TotalQuantity, _options.UseSavings, _options.UseSwapPools);
    }

    private IAlgoCommand CreateEntryBuy(SymbolData item, IList<PositionLot> lots)
    {
        // only look at symbols without a full lot
        if (lots.Count > 0)
        {
            return Noop();
        }

        // identify the entry price
        if (!item.Klines.TryGetPriceForRsi(x => x.ClosePrice, _options.Rsi.Buy.Periods, _options.Rsi.Buy.Oversold, out var price))
        {
            return Noop();
        }
        price = price.AdjustPriceDownToTickSize(item.Symbol);

        // identify the appropriate buy quantity for this price
        var quantity = CalculateBuyQuantity(item, price);

        // create the limit order
        return EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, null, price, null, null, _options.UseSavings, _options.UseSwapPools);
    }

    private IAlgoCommand CreateTopUpBuy(SymbolData item, IList<PositionLot> lots)
    {
        // there must be something to top up
        if (lots.Count == 0)
        {
            return Noop();
        }

        // the symbol must not be overbought
        var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Buy.Periods);
        if (rsi >= _options.Rsi.Buy.Overbought)
        {
            return Noop();
        }

        // only ever top up the highest position - recovery is handled elsewhere
        var maxPrice = lots.Max(x => x.AvgPrice);
        var lastLot = lots[0];
        if (lastLot.AvgPrice < maxPrice)
        {
            LogTopUpSkippedSymbolARecoveryBuy(TypeName, Context.Name, item.Symbol.Name, item.AutoPosition.Positions.Last.Quantity, item.Symbol.BaseAsset, item.AutoPosition.Positions.Last.Price, item.Symbol.QuoteAsset);
            return Noop();
        }

        // skip symbols below min required for top up
        var price = lastLot.AvgPrice * (1 + _options.MinChangeFromLastPositionPriceRequiredForTopUpBuy);
        price = price.AdjustPriceUpToTickSize(item.Symbol);

        if (item.Ticker.ClosePrice < price)
        {
            LogTopUpSkippedSymbolWithPriceNotHighEnough(TypeName, Context.Name, item.Symbol.Name, item.Ticker.ClosePrice, price, _options.MinChangeFromLastPositionPriceRequiredForTopUpBuy, item.AutoPosition.Positions.Last.Price);
            return Noop();
        }

        // raise to the current ticker if needed for the order to go through
        price = Math.Max(item.Ticker.ClosePrice, price);

        // if we got here then we have a top up
        LogTopUpElectedSymbol(TypeName, Context.Name, item.Name, item.Ticker.ClosePrice, item.Symbol.QuoteAsset);

        // identify the appropriate buy quantity for this price
        var quantity = CalculateBuyQuantity(item, price);

        // skip if there is already an open order at an equal or higher ticker to avoid order twitching
        if (item.Orders.Open.Any(x => x.Side == OrderSide.Buy && x.Type == OrderType.Limit && x.OriginalQuantity == quantity && x.Price >= price))
        {
            return Noop();
        }

        // create the limit order
        return EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, null, price, null, null, _options.UseSavings, _options.UseSwapPools);
    }

    private IAlgoCommand CreateRecoveryBuy(SymbolData item, IList<PositionLot> lots)
    {
        // recovery must be enabled
        if (!_options.Recovery.Enabled)
        {
            return CancelRecoveryBuy(item.Symbol);
        }

        // there must something to recover
        if (lots.Count == 0)
        {
            return CancelRecoveryBuy(item.Symbol);
        }

        // calculate the price required for the recovery rsi
        if (!item.Klines.TryGetPriceForRsi(x => x.ClosePrice, _options.Recovery.Rsi.Periods, _options.Recovery.Rsi.Buy, out var buyPrice))
        {
            return CancelRecoveryBuy(item.Symbol);
        }
        buyPrice = buyPrice.AdjustPriceDownToTickSize(item.Symbol);

        // the buy price must be low enough from the last buy for a recovery buy to take place
        var lastLot = lots[0];
        var dropPrice = lastLot.AvgPrice * (1 - _options.Recovery.DropRate);
        if (buyPrice >= dropPrice)
        {
            return CancelRecoveryBuy(item.Symbol);
        }

        // ticker must be below the buy price to bother reserving funds
        if (item.Ticker.ClosePrice >= buyPrice)
        {
            return CancelRecoveryBuy(item.Symbol);
        }

        // calculate the quantity
        var quantity = CalculateBuyQuantity(item, buyPrice);

        LogRecoveryPlacingBuy(TypeName, Context.Name, quantity, buyPrice, item.Symbol.BaseAsset, item.Symbol.QuoteAsset);
        return EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.FillOrKill, quantity, null, buyPrice, null, RecoveryBuyTag, _options.UseSavings, _options.UseSwapPools);
    }

    private IAlgoCommand CancelRecoveryBuy(Symbol symbol)
    {
        return CancelOpenOrders(symbol, OrderSide.Buy, null, RecoveryBuyTag);
    }

    private IAlgoCommand CreateRecoverySell(SymbolData item, IList<PositionLot> lots, PositionStats stats)
    {
        // recovery must be enabled
        if (!_options.Recovery.Enabled)
        {
            return CancelRecoverySell(item.Symbol);
        }

        // there must be something to recover
        if (lots.Count == 0)
        {
            return CancelRecoverySell(item.Symbol);
        }

        // identify the recovery sell price for the target rsi
        if (!item.Klines.TryGetPriceForRsi(x => x.ClosePrice, _options.Recovery.Rsi.Periods, _options.Recovery.Rsi.Sell, out var sellPrice))
        {
            return CancelRecoverySell(item.Symbol);
        }
        sellPrice = sellPrice.AdjustPriceUpToTickSize(item.Symbol);

        // the last lot must be under the recovery sell price
        var lastLot = lots[0];
        if (lastLot.AvgPrice >= sellPrice)
        {
            return CancelRecoverySell(item.Symbol);
        }

        // there must be a local max from the last lot above the recovery sell price
        var maxPrice = 0M;
        foreach (var lot in lots)
        {
            if (lot.AvgPrice >= maxPrice)
            {
                maxPrice = lot.AvgPrice;
            }
            else
            {
                break;
            }
        }
        if (maxPrice <= sellPrice)
        {
            return CancelRecoverySell(item.Symbol);
        }

        // gather all the lots that fit under the sell price up to the local max
        var quantity = 0M;
        var notional = 0M;
        var electedQuantity = 0M;
        var lastPrice = 0M;
        foreach (var lot in lots)
        {
            // keep adding everything up so we get a average from the end
            quantity += lot.Quantity;
            notional += lot.Quantity * lot.AvgPrice;

            // continue until the quantity is sellable
            if (quantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                continue;
            }

            // continue until the notional is sellable
            if (notional < item.Symbol.Filters.MinNotional.MinNotional)
            {
                continue;
            }

            // only add the lot if the price is greater than or equal to the last price
            if (lot.AvgPrice < lastPrice)
            {
                break;
            }
            lastPrice = lot.AvgPrice;

            // only add the lot if the price is under or equal to the local max
            if (lot.AvgPrice > maxPrice)
            {
                break;
            }

            // the average cost price must fit under the sell price to break even
            var avgPrice = notional / quantity;
            avgPrice *= 1 + (2 * _options.FeeRate);
            avgPrice = avgPrice.AdjustPriceUpToTickSize(item.Symbol);
            if (avgPrice <= sellPrice)
            {
                // keep the candidate quantity and continue looking for more
                electedQuantity = quantity;
            }
        }

        // if we found something to sell then place the recovery sell
        if (electedQuantity > 0)
        {
            LogRecoveryPlacingSell(TypeName, Context.Name, electedQuantity, item.Symbol.BaseAsset, sellPrice, item.Symbol.QuoteAsset);
            return EnsureSingleOrder(item.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, electedQuantity, null, sellPrice, null, RecoverySellTag, _options.UseSavings, _options.UseSwapPools);
        }

        return Noop();
    }

    private IAlgoCommand CancelRecoverySell(Symbol symbol)
    {
        return CancelOpenOrders(symbol, OrderSide.Sell, null, RecoverySellTag);
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
        notional *= 1 + _options.FeeRate;

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

    [LoggerMessage(29, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with lowest unrealized absolute pnl {UnrealizedAbsolutePnl:F8}")]
    private partial void LogSymbolWithLowestAbsolutePnl(string type, string name, string symbol, decimal unrealizedAbsolutePnl);

    [LoggerMessage(30, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with lowest unrealized relative pnl {UnrealizedRelativePnl:P2}")]
    private partial void LogSymbolWithLowestRelativePnl(string type, string name, string symbol, decimal unrealizedRelativePnl);

    [LoggerMessage(29, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with highest unrealized absolute pnl {UnrealizedAbsolutePnl:F8}")]
    private partial void LogSymbolWithHighestAbsolutePnl(string type, string name, string symbol, decimal unrealizedAbsolutePnl);

    [LoggerMessage(30, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with highest unrealized relative pnl {UnrealizedRelativePnl:P2}")]
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

    [LoggerMessage(41, LogLevel.Error, "{Type} {Name} recovery buy detected lot step size for symbol {Symbol} is zero")]
    private partial void LogRecoveryBuyDetectedZeroLotStepSize(string type, string name, string symbol);

    [LoggerMessage(42, LogLevel.Information, "{Type} {Name} recovery placing recovery buy of {Quantity:F8} {Asset} at {BuyPrice:F8} {Quote}")]
    private partial void LogRecoveryPlacingBuy(string type, string name, decimal quantity, decimal buyPrice, string asset, string quote);

    [LoggerMessage(43, LogLevel.Warning, "{Type} {Name} recovery cannot place buy to recover lot of {Quantity:F8} {Asset} bought at {BuyPrice:F8} {Quote} with current settings")]
    private partial void LogRecoveryCannotPlaceBuy(string type, string name, decimal quantity, decimal buyPrice, string asset, string quote);

    [LoggerMessage(44, LogLevel.Information, "{Type} {Name} recovery placing recovery sell of {Quantity:F8} {Asset} at {SellPrice:F8} {Quote}")]
    private partial void LogRecoveryPlacingSell(string type, string name, decimal quantity, string asset, decimal sellPrice, string quote);

    [LoggerMessage(45, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} with a recovery buy of {Quantity:F8} {Asset} at {Price:F8} {Quote}")]
    private partial void LogTopUpSkippedSymbolARecoveryBuy(string type, string name, string symbol, decimal quantity, string asset, decimal price, string quote);

    [LoggerMessage(46, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} above break even with (PnL: {UnrealizedPnl:P2}, Unrealized: {UnrealizedAbsPnl:F8} {Quote}, PV: {PV:F8} {Quote}")]
    private partial void LogSymbolAtBreakEven(string type, string name, string symbol, decimal unrealizedPnl, decimal unrealizedAbsPnl, string quote, decimal pv);

    #endregion Logging
}