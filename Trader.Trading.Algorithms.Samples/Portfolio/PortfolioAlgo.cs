using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Outcompute.Trader.Trading.Commands;
using static System.Collections.Generic.KdjExtensions;

namespace Outcompute.Trader.Trading.Algorithms.Samples.Portfolio;

public partial class PortfolioAlgo : Algo
{
    private readonly IOptionsMonitor<PortfolioAlgoOptions> _monitor;
    private readonly ILogger _logger;

    public PortfolioAlgo(IOptionsMonitor<PortfolioAlgoOptions> monitor, ILogger<PortfolioAlgo> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    private const string TypeName = nameof(PortfolioAlgo);
    private const string SellTag = "Sell";
    private const string TrailingTag = "Trailing";
    private const string BuyTag = "Buy";

    private PortfolioAlgoOptions _options = null!;

    protected override IAlgoCommand OnExecute()
    {
        // always get the latest options so the user can change them in real-time
        _options = _monitor.Get(Context.Name);

        var command = Noop();
        var lookup = new Dictionary<string, PositionStats>();

        foreach (var item in Context.Data)
        {
            if (item.Symbol.Filters.LotSize.StepSize <= 0)
            {
                LogSkippedSymbolWithInvalidLotStepSize(TypeName, Context.Name, item.Symbol.Name, item.Symbol.Filters.LotSize.StepSize, item.Symbol.BaseAsset);
                continue;
            }

            // get sellable lots from the end
            var lots = item.AutoPosition.Positions.Reverse().EnumerateLots(item.Symbol.Filters.LotSize.StepSize).ToList();

            // get the stats for sellable lots
            var stats = lots.GetStats(item.Ticker.ClosePrice);

            // cache for the reporting method
            lookup[item.Symbol.Name] = stats;

            // skip symbol with invalid data
            if (!item.IsValid)
            {
                LogSkippedInvalidatedSymbol(TypeName, Context.Name, item.Symbol.Name);
                continue;
            }

            command = command.Then(Sell(item, lots, stats));
            command = command.Then(Buy(item, lots));
        }

        ReportAggregateStats(lookup);

        return command;
    }

    // todo: move all this code to the algo stats publisher so it does not consume algo time
    private void ReportAggregateStats(Dictionary<string, PositionStats> lookup)
    {
        var reportable = Context.Data.Where(x => x.IsValid);

        var grouped = reportable
            .GroupBy(x => x.Symbol.QuoteAsset);

        // report on total portfolio value for each quote
        foreach (var quote in grouped)
        {
            // get stats for every sellable symbol
            var stats = quote.Select(x => (x.Symbol, Stats: lookup[x.Symbol.Name]));

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
                cost == 0M ? 0M : (pv - cost) / cost,
                pv - cost,
                rpnl,
                pv - cost + rpnl);

            // every unsellable symbol with opening disabled
            foreach (var item in stats
                .Where(x => x.Stats.TotalQuantity < x.Symbol.Filters.LotSize.MinQuantity)
                .Where(x => _options.Buying.Opening.ExcludeSymbols.Contains(x.Symbol.Name)))
            {
                LogClosedSymbol(TypeName, Context.Name, item.Symbol.Name, item.Stats.TotalQuantity, item.Symbol.BaseAsset, item.Stats.PresentValue, item.Symbol.QuoteAsset);
            }

            // every non zero symbol by relative pnl
            foreach (var item in stats
                .Where(x => x.Stats.TotalQuantity > 0)
                .OrderBy(x => x.Stats.RelativePnL))
            {
                LogSymbolPv(TypeName, Context.Name, item.Symbol.Name, item.Stats.RelativePnL, item.Stats.AbsolutePnL, item.Symbol.QuoteAsset, item.Stats.PresentValue);
            }

            // report on the absolute loser
            var absLoser = stats
                .OrderBy(x => x.Stats.AbsolutePnL)
                .FirstOrDefault();

            if (!IsNullOrEmpty(absLoser.Symbol?.Name))
            {
                LogSymbolWithLowestAbsolutePnl(TypeName, Context.Name, absLoser.Symbol.Name, absLoser.Stats.AbsolutePnL);
            }

            // report on the relative loser
            var relLoser = stats
                .OrderBy(x => x.Stats.RelativePnL)
                .FirstOrDefault();

            if (!IsNullOrEmpty(relLoser.Symbol?.Name))
            {
                LogSymbolWithLowestRelativePnl(TypeName, Context.Name, relLoser.Symbol.Name, relLoser.Stats.RelativePnL);
            }

            // report on the absolute winner
            var absWinner = stats
                .OrderByDescending(x => x.Stats.AbsolutePnL)
                .FirstOrDefault();

            if (!IsNullOrEmpty(absWinner.Symbol?.Name))
            {
                LogSymbolWithHighestAbsolutePnl(TypeName, Context.Name, absWinner.Symbol.Name, absWinner.Stats.AbsolutePnL);
            }

            // report on the relative loser
            var relWinner = stats
                .OrderByDescending(x => x.Stats.RelativePnL)
                .FirstOrDefault();

            if (!IsNullOrEmpty(relWinner.Symbol?.Name))
            {
                LogSymbolWithHighestRelativePnl(TypeName, Context.Name, relWinner.Symbol.Name, relWinner.Stats.RelativePnL);
            }

            // report on the highest sellable pv
            var highPv = stats
                .Where(x => x.Stats.TotalQuantity > 0)
                .Where(x => !_options.Selling.ExcludeSymbols.Contains(x.Symbol.Name))
                .OrderByDescending(x => x.Stats.PresentValue)
                .FirstOrDefault();

            if (!IsNullOrEmpty(highPv.Symbol?.Name))
            {
                LogSymbolWithHighestPresentValue(TypeName, Context.Name, highPv.Symbol.Name, highPv.Stats.PresentValue, highPv.Symbol.QuoteAsset);
            }

            // report on sellable pv
            var highPvBreakEven = stats
                .Where(x => !_options.Selling.ExcludeSymbols.Contains(x.Symbol.Name))
                .Where(x => x.Stats.TotalQuantity > 0)
                .Where(x => x.Stats.RelativePnL >= 0)
                .OrderByDescending(x => x.Stats.PresentValue)
                .FirstOrDefault();

            if (!IsNullOrEmpty(highPvBreakEven.Symbol?.Name))
            {
                LogSymbolWithHighestPresentValueAboveBreakEven(TypeName, Context.Name, highPvBreakEven.Symbol.Name, highPvBreakEven.Stats.PresentValue, highPvBreakEven.Symbol.QuoteAsset);
            }
        }
    }

    private IAlgoCommand Buy(SymbolData item, IList<PositionLot> lots)
    {
        IAlgoCommand Clear() => CancelOpenOrders(item.Symbol, OrderSide.Buy, null, BuyTag);

        // buying must be enabled
        if (!_options.Buying.Enabled)
        {
            return Clear();
        }

        // if the symbol is on the opening exclusion list then it must have positions
        if (lots.Count == 0 && _options.Buying.Opening.ExcludeSymbols.Contains(item.Symbol.Name))
        {
            LogBuySkippedSymbolOnOpeningExclusionSet(TypeName, Context.Name, item.Symbol.Name);
            return Clear();
        }

        // skip if the symbol is on buy cooldown
        if (lots.Count > 0 && lots[0].Time.Add(_options.Buying.Cooldown) >= Context.TickTime)
        {
            return Clear();
        }

        // guard - rsi set must be high enough to risk funds
        var shortRsi = item.Klines.Rsi(x => x.ClosePrice, 6).Last();
        var medRsi = item.Klines.Rsi(x => x.ClosePrice, 12).Last();
        var longRsi = item.Klines.Rsi(x => x.ClosePrice, 24).Last();
        var rsiOK = shortRsi >= 20 && medRsi >= 20 && longRsi >= 20;
        if (!rsiOK)
        {
            return Noop();
        }

        var stopPrice = decimal.MaxValue;

        // detect a kdj cross
        var kdj = item.Klines.Kdj().TakeLast(2).ToArray();
        if (kdj[0].Side == KdjSide.Down && kdj[1].Side == KdjSide.Up)
        {
            return CreateMarketBuy(item);
        }

        /*
        // place the default buy at the super trend high
        var trend = item.Klines.SuperTrend().Last();
        if (trend.Direction == SuperTrendDirection.Down)
        {
            stopPrice = Math.Min(stopPrice, item.Symbol.RaisePriceToTickSize(trend.High));
        }*/

        /*
        // attempt to identify the price needed for upwards cross
        if (item.Klines.TryGetMacdForUpcross(x => x.ClosePrice, out var cross))
        {
            stopPrice = Math.Min(stopPrice, item.Symbol.RaisePriceToTickSize(cross.Price));
        }
        */

        /*
        // handle a sudden macd reversal
        var macds = item.Klines.Macd(x => x.ClosePrice).TakeLast(2).ToArray();
        if (macds[0].IsUptrend && macds[1].IsDowntrend && item.Klines.TryGetMacdForUpcross(x => x.ClosePrice, out var reversal))
        {
            stopPrice = Math.Min(stopPrice, item.Symbol.RaisePriceToTickSize(reversal.Price * 1.01M));
        }
        */

        // lower to an extreme outlier of opportunity
        var band = item.Klines.BollingerBands(x => x.ClosePrice, 20, 5).Last();
        if (item.Ticker.LowPrice <= band.Low)
        {
            stopPrice = Math.Min(stopPrice, item.Symbol.RaisePriceToTickSize(item.Ticker.ClosePrice * 1.01M));
        }

        // skip if no stop buy can be calculated
        if (stopPrice == decimal.MaxValue)
        {
            return Noop();
        }

        // skip if there is already a buy a lower price
        if (item.Orders.Open.Any(x => x.Side == OrderSide.Buy && x.StopPrice < stopPrice))
        {
            return Noop();
        }

        // only bother reserving funds if the ticker is close enough
        var distance = Math.Abs((stopPrice - item.Ticker.ClosePrice) / item.Ticker.ClosePrice);
        if (distance > _options.Buying.ActivationRate)
        {
            return Noop();
        }

        // calculate the buy price window from the stop
        var buyPrice = item.Symbol.RaisePriceToTickSize(stopPrice * (1 + _options.Buying.BuyWindowRate));

        // define the quantity to buy
        var quantity = CalculateBuyQuantity(item, buyPrice, _options.Buying.BalanceRate);

        // define the notional to lock
        var notional = quantity * buyPrice;

        // define the notional to free
        var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Sell).Sum(x => x.OriginalQuantity);
        var required = Math.Max(notional - locked, 0);

        // place the order
        return Sequence(
            EnsureSpotBalance(item.Symbol.QuoteAsset, required, _options.UseSavings, _options.UseSwapPools),
            EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.StopLossLimit, TimeInForce.GoodTillCanceled, quantity, null, buyPrice, stopPrice, BuyTag));
    }

    private IAlgoCommand CreateMarketBuy(SymbolData item)
    {
        // define the quantity to buy
        var quantity = CalculateBuyQuantity(item, item.Ticker.ClosePrice, _options.Buying.BalanceRate);

        // define the notional to lock
        var notional = quantity * item.Ticker.ClosePrice;

        // define the notional to free
        var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Sell).Sum(x => x.OriginalQuantity);
        var required = Math.Max(notional - locked, 0);

        // place the order
        return Sequence(
            EnsureSpotBalance(item.Symbol.QuoteAsset, required, _options.UseSavings, _options.UseSwapPools),
            CancelOpenOrders(item.Symbol, OrderSide.Buy, null, BuyTag),
            EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Market, null, quantity, null, null, null, BuyTag));
    }

    private IAlgoCommand Sell(SymbolData item, IEnumerable<PositionLot> lots, PositionStats stats)
    {
        IAlgoCommand Clear() => Sequence(
            CancelOpenOrders(item.Symbol, OrderSide.Sell, _options.Selling.SellWindowRate, SellTag),
            CancelOpenOrders(item.Symbol, OrderSide.Sell, _options.Selling.SellWindowRate, TrailingTag));

        // selling must be enabled
        if (!_options.Selling.Enabled)
        {
            return Clear();
        }

        // symbol must not be on the selling exclusion set
        if (_options.Selling.ExcludeSymbols.Contains(item.Symbol.Name))
        {
            return Clear();
        }

        // there must be something to sell
        if (stats.TotalQuantity == 0)
        {
            return Clear();
        }

        // apply kdj rules
        var kdj = item.Klines.Kdj().TakeLast(2).ToArray();

        // detect a kdj peak cross
        if (kdj[0].Side == KdjSide.Up && kdj[1].Side == KdjSide.Up &&
            kdj[0].J > 100 && kdj[1].J < 100)
        {
            return CreateMarketSell(item, stats, lots);
        }

        // detect a kdj normal cross
        if (kdj[0].Side == KdjSide.Up && kdj[1].Side == KdjSide.Down)
        {
            return CreateMarketSell(item, stats, lots);
        }

        /*
        // place the default stop loss at the trend low band if possible
        var stopPrice = 0M;
        var trend = item.Klines.SkipLast(1).SuperTrend().Last();
        if (trend.Direction == SuperTrendDirection.Up)
        {
            stopPrice = item.Symbol.LowerPriceToTickSize(trend.Low);
        }
        */

        /*
        // attempt to identify the price needed for downwards cross on the current period
        // only apply this rule if we dont have buys in the same period to avoid order twitching
        if (item.Klines.SkipLast(1).TryGetMacdForDowncross(x => x.ClosePrice, out var cross))
        {
            // raise the stop loss price to the cross price so we exit earlier
            stopPrice = Math.Max(stopPrice, item.Symbol.LowerPriceToTickSize(cross.Price));
        }
        */

        // raise to a higher trailing stop for extreme outliers
        var stopPrice = 0M;
        var band = item.Klines.BollingerBands(x => x.ClosePrice, 20, 3).Last();
        if (item.Ticker.ClosePrice >= band.High)
        {
            stopPrice = Math.Max(stopPrice, item.Symbol.LowerPriceToTickSize(item.Ticker.ClosePrice * 0.99M));
        }

        // raise to a safety stop loss for reversals
        var maxSafetyStop = item.Symbol.LowerPriceToTickSize(stats.AvgPrice * 1.01M);
        var currentSafetyStop = item.Symbol.LowerPriceToTickSize(item.Ticker.ClosePrice * 0.99M);
        var safetyStop = Math.Min(maxSafetyStop, currentSafetyStop);
        stopPrice = Math.Max(stopPrice, safetyStop);

        // raise to a loose trailing stop loss
        //stopPrice = Math.Max(stopPrice, item.Symbol.LowerPriceToTickSize(item.Ticker.ClosePrice * 0.9M));

        // skip if we cant calculate the stop loss at all
        if (stopPrice <= 0)
        {
            //LogCannotCalculateStopLoss(TypeName, Context.Name, item.Symbol.Name);
            return Noop();
        }

        // set the sell window price
        var quantity = item.Symbol.LowerQuantityToLotStepSize(stats.TotalQuantity);
        var sellPrice = item.Symbol.LowerPriceToTickSize(stopPrice * (1 - _options.Selling.SellWindowRate));
        var notional = item.Symbol.LowerPriceToTickSize(quantity * sellPrice);

        if (quantity < item.Symbol.Filters.LotSize.MinQuantity)
        {
            LogCannotPlaceSellOrderWithQuantityUnderMin(TypeName, Context.Name, item.Symbol.Name, quantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
            return Noop();
        }

        if (notional < item.Symbol.Filters.MinNotional.MinNotional)
        {
            LogCannotPlaceSellOrderWithNotionalUnderMin(TypeName, Context.Name, item.Symbol.Name, notional, item.Symbol.QuoteAsset, item.Symbol.Filters.MinNotional.MinNotional);
            return Noop();
        }

        // skip if there is already a stop at a higher price
        if (item.Orders.Open.Any(x => x.Side == OrderSide.Sell && x.StopPrice > stopPrice))
        {
            return Noop();
        }

        // take any current sell orders into account for spot balance release
        var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Sell).Sum(x => x.OriginalQuantity);
        var required = Math.Max(quantity - locked, 0);

        // create the command sequence for the exchange
        return Sequence(
            EnsureSpotBalance(item.Symbol.BaseAsset, required, _options.UseSavings, _options.UseSwapPools),
            EnsureSingleOrder(item.Symbol, OrderSide.Sell, OrderType.StopLossLimit, TimeInForce.GoodTillCanceled, quantity, null, sellPrice, stopPrice, SellTag));
    }

    private IAlgoCommand CreateMarketSell(SymbolData item, PositionStats stats, IEnumerable<PositionLot> lots)
    {
        // set the sell window price
        var quantity = item.Symbol.LowerQuantityToLotStepSize(stats.TotalQuantity);
        /*
        if (!TryGetElectedQuantity(item, lots, item.Ticker.ClosePrice, out var quantity))
        {
            return Noop();
        }
        */

        var sellPrice = item.Ticker.ClosePrice;
        var notional = item.Symbol.LowerPriceToTickSize(quantity * sellPrice);

        if (quantity < item.Symbol.Filters.LotSize.MinQuantity)
        {
            LogCannotPlaceSellOrderWithQuantityUnderMin(TypeName, Context.Name, item.Symbol.Name, quantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
            return Noop();
        }

        if (notional < item.Symbol.Filters.MinNotional.MinNotional)
        {
            LogCannotPlaceSellOrderWithNotionalUnderMin(TypeName, Context.Name, item.Symbol.Name, notional, item.Symbol.QuoteAsset, item.Symbol.Filters.MinNotional.MinNotional);
            return Noop();
        }

        // take any current sell orders into account for spot balance release
        var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Sell).Sum(x => x.OriginalQuantity);
        var required = Math.Max(quantity - locked, 0);

        // create the command sequence for the exchange
        return Sequence(
            EnsureSpotBalance(item.Symbol.BaseAsset, required, _options.UseSavings, _options.UseSwapPools),
            CancelOpenOrders(item.Symbol, OrderSide.Sell, null, SellTag),
            EnsureSingleOrder(item.Symbol, OrderSide.Sell, OrderType.Market, null, quantity, null, null, null, SellTag));
    }

    private static bool TryGetElectedQuantity(SymbolData item, IEnumerable<PositionLot> lots, decimal sellPrice, out decimal electedQuantity)
    {
        // gather the all the lots that fit under the sell price
        var quantity = 0M;
        var buyNotional = 0M;
        var sellNotional = 0M;

        electedQuantity = 0M;

        foreach (var lot in lots)
        {
            // keep adding everything up so we get a average from the end
            quantity += lot.Quantity;
            buyNotional += lot.Quantity * lot.AvgPrice;
            sellNotional += lot.Quantity * sellPrice;

            // adjust the tested quantity to the asset precision
            //var adjustedQuantity = item.Symbol.LowerToBaseAssetPrecision(quantity);
            //var leftoverQuantity = quantity - adjustedQuantity;
            //var adjustedBuyNotional = item.Symbol.LowerPriceToTickSize(buyNotional - leftoverQuantity * lot.AvgPrice);
            //var adjustedSellNotional = item.Symbol.LowerPriceToTickSize(sellNotional - leftoverQuantity * sellPrice);

            // continue until the quantity is sellable
            if (quantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                continue;
            }

            // continue until the notional is sellable
            if (sellNotional < item.Symbol.Filters.MinNotional.MinNotional)
            {
                continue;
            }

            // the average cost price must fit under the profit price
            var avgPrice = buyNotional / quantity;
            avgPrice = item.Symbol.RaisePriceToTickSize(avgPrice);
            if (avgPrice > sellPrice)
            {
                continue;
            }

            // keep the candidate quantity and continue looking for more
            electedQuantity = quantity;
        }

        return electedQuantity > 0;
    }

    private decimal CalculateBuyQuantity(SymbolData item, decimal price, decimal balanceRate)
    {
        // calculate the notional to use for buying
        var notional = item.Spot.QuoteAsset.Free
            + (_options.UseSavings ? item.Savings.QuoteAsset.FreeAmount : 0)
            + (_options.UseSwapPools ? item.SwapPools.QuoteAsset.Total : 0);

        notional *= balanceRate;

        // raise to a valid number
        notional = item.Symbol.RaiseTotalUpToMinNotional(notional, 2);
        notional = item.Symbol.RaisePriceToTickSize(notional);

        // calculate the quantity for the limit order
        var quantity = notional / price;

        // raise the quantity to a valid number
        quantity = item.Symbol.RaiseQuantityToMinLotSize(quantity);
        quantity = item.Symbol.RaiseQuantityToLotStepSize(quantity);

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

    [LoggerMessage(16, LogLevel.Information, "{Type} {Name} sell off skipped symbol {Symbol} because it is in the exclusion set")]
    private partial void LogSellOffSkippedSymbolOnExclusionSet(string type, string name, string symbol);

    [LoggerMessage(17, LogLevel.Information, "{Type} evaluating symbols for pump sell")]
    private partial void LogEvaluatingSymbolsForPumpSell(string type);

    [LoggerMessage(18, LogLevel.Information, "{Type} skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} lower than sell RSI({Periods}) {SellRSI:F8}")]
    private partial void LogSkippedSymbolWithLowSellRsi(string type, string symbol, int periods, decimal rsi, decimal sellRsi);

    [LoggerMessage(19, LogLevel.Information, "{Type} {Name} sell off elected symbol {Symbol}")]
    private partial void LogSellOffElectedSymbol(string type, string name, string symbol);

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

    [LoggerMessage(25, LogLevel.Information, "{Type} {Name} sell off skipped symbol {Symbol} because selling off is disabled")]
    private partial void LogSellOffDisabled(string type, string name, string symbol);

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

    [LoggerMessage(35, LogLevel.Information, "{Type} {Name} sell off skipped symbol {Symbol} with quantity {Quantity:F8} {Asset} under min lot size of {MinLotSize:F8}")]
    private partial void LogSellOffSkippedSymbolWithQuantityUnderMinLotSize(string type, string name, string symbol, decimal quantity, string asset, decimal minLotSize);

    [LoggerMessage(36, LogLevel.Information, "{Type} {Name} sell off skipped symbol {Symbol} with present value {PV:F8} {Quote} under min notional {MinNotional:F8} {Quote}")]
    private partial void LogSellOffSkippedSymbolWithPresentValueUnderMinNotional(string type, string name, string symbol, decimal pv, decimal minNotional, string quote);

    [LoggerMessage(37, LogLevel.Information, "{Type} {Name} sell off skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} under sell RSI({Periods}) {SellRSI:F8}")]
    private partial void LogSellOffSkippedSymbolWithLowSellRsi(string type, string name, string symbol, int periods, decimal rsi, decimal sellRsi);

    [LoggerMessage(38, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} with quantity {Quantity:F8} {Asset} above min {MinLotSize:F8} {Asset} and notional {Cost:F8} above min {MinNotional:F8} {Quote}")]
    private partial void LogEntryBuySkippedSymbolWithQuantityAndNotionalAboveMin(string type, string name, string symbol, decimal quantity, string asset, decimal minLotSize, decimal cost, decimal minNotional, string quote);

    [LoggerMessage(39, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} on cooldown until {Cooldown}")]
    private partial void LogEntryBuySkippedSymbolOnCooldown(string type, string name, string symbol, DateTime cooldown);

    [LoggerMessage(40, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} with RSI({Periods}) {RSI:F8} above oversold of {Oversold:F8}")]
    private partial void LogEntryBuySkippedSymbolWithRsiAboveOversold(string type, string name, string symbol, int periods, decimal rsi, decimal oversold);

    [LoggerMessage(41, LogLevel.Error, "{Type} {Name} recovery buy detected lot step size for symbol {Symbol} is zero")]
    private partial void LogRecoveryBuyDetectedZeroLotStepSize(string type, string name, string symbol);

    [LoggerMessage(42, LogLevel.Information, "{Type} {Name} recovery buy placing {OrderType} {OrderSide} of {Quantity:F8} {Asset} at {BuyPrice:F8} {Quote}")]
    private partial void LogRecoveryBuyPlacingOrder(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, decimal buyPrice, string asset, string quote);

    [LoggerMessage(43, LogLevel.Warning, "{Type} {Name} recovery cannot place buy to recover lot of {Quantity:F8} {Asset} bought at {BuyPrice:F8} {Quote} with current settings")]
    private partial void LogRecoveryCannotPlaceBuy(string type, string name, decimal quantity, decimal buyPrice, string asset, string quote);

    [LoggerMessage(44, LogLevel.Information, "{Type} {Name} {Symbol} sell elected symbol for selling at {Quantity:F8} {Asset} with stop at {StopPrice:F8} {Quote} and profit at {ProfitPrice:F8} {Quote}")]
    private partial void LogRecoverySellElectedSymbol(string type, string name, string symbol, decimal quantity, string asset, decimal stopPrice, decimal profitPrice, string quote);

    [LoggerMessage(45, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} with a recovery buy of {Quantity:F8} {Asset} at {Price:F8} {Quote}")]
    private partial void LogTopUpSkippedSymbolWithRecoveryBuy(string type, string name, string symbol, decimal quantity, string asset, decimal price, string quote);

    [LoggerMessage(46, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with (PnL: {UnrealizedPnl:P2}, Unrealized: {UnrealizedAbsPnl:F8} {Quote}, PV: {PV:F8} {Quote}")]
    private partial void LogSymbolPv(string type, string name, string symbol, decimal unrealizedPnl, decimal unrealizedAbsPnl, string quote, decimal pv);

    [LoggerMessage(47, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} because top up buying is disabled")]
    private partial void LogTopUpDisabled(string type, string name, string symbol);

    [LoggerMessage(48, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} because there is no full lot of at least {Size} {Asset} to top up")]
    private partial void LogTopUpSkippedSymbolWithoutFullLot(string type, string name, string symbol, decimal size, string asset);

    [LoggerMessage(47, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} because the RSI({Periods}) of {RSI:F2} is over the threshold of {Overbought:F2}")]
    private partial void LogTopUpSkippedSymbolWithOverboughtRsi(string type, string name, string symbol, int periods, decimal rsi, decimal overbought);

    [LoggerMessage(48, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} because the ticker of {Ticker:F8} {Asset} is above the safety SMA({Periods}) of {SMA:F8} {Asset}")]
    private partial void LogTopUpSkippedSymbolWithTickerAboveSafetySma(string type, string name, string symbol, decimal ticker, string asset, int periods, decimal sma);

    [LoggerMessage(49, LogLevel.Information, "{Type} {Name} sell off skipped symbol {Symbol} with relative value of {RV:P2} under the trigger of {Trigger:P2}")]
    private partial void LogSellOffSkippedSymbolWithRelativeValueUnderTrigger(string type, string name, string symbol, decimal rv, decimal trigger);

    [LoggerMessage(50, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} because recovery is disabled")]
    private partial void LogRecoverySellDisabled(string type, string name, string symbol);

    [LoggerMessage(51, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} on the exclusion set")]
    private partial void LogRecoverySellSkippedSymbolOnExclusionSet(string type, string name, string symbol);

    [LoggerMessage(53, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} due to inability to identify price for RSI({Periods}) {RSI:F2}")]
    private partial void LogRecoverySellSkippedSymbolWithUnknownRsiPrice(string type, string name, string symbol, int periods, decimal rsi);

    [LoggerMessage(54, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} with last lot price of {LastLotPrice:F8} {Quote} not under the recovery sell price of {SellPrice:F8} {Quote}")]
    private partial void LogRecoverySellSkippedSymbolWithHighLastLotPrice(string type, string name, string symbol, decimal lastLotPrice, string quote, decimal sellPrice);

    [LoggerMessage(55, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} because last lot price of {LastLotPrice:F8} {Quote} does not have a local max")]
    private partial void LogRecoverySellSkippedSymbolWithoutLocalMax(string type, string name, string symbol, decimal lastLotPrice, string quote);

    [LoggerMessage(56, LogLevel.Information, "{Type} {Name} {Symbol} sell skipped symbol because it could not fit any lot under the profit price of {ProfitPrice:F8} {Quote}")]
    private partial void LogSellSkippedSymbolWithLotsNotUnderProfitPrice(string type, string name, string symbol, decimal profitPrice, string quote);

    [LoggerMessage(57, LogLevel.Information, "{Type} {Name} {Symbol} buy step skipped symbol because entry buying is disabled")]
    private partial void LogBuyDisabled(string type, string name, string symbol);

    [LoggerMessage(58, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} because it already has {Lots} lots totalling {Quantity:F8} {Asset}")]
    private partial void LogEntryBuySkippedSymbolWithLots(string type, string name, string symbol, int lots, decimal quantity, string asset);

    [LoggerMessage(59, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} because it could identify the price for RSI({Periods}) {RSI:F2}")]
    private partial void LogEntryBuySkippedSymbolWithUnknownRsiPrice(string type, string name, string symbol, int periods, decimal rsi);

    [LoggerMessage(60, LogLevel.Information, "{Type} {Name} recovery buy skipped symbol {Symbol} because recovery is disabled")]
    private partial void LogBuyRecoveryDisabled(string type, string name, string symbol);

    [LoggerMessage(61, LogLevel.Information, "{Type} {Name} recovery buy skipped symbol {Symbol} on the exclusion set")]
    private partial void LogRecoveryBuySkippedSymbolOnExclusionSet(string type, string name, string symbol);

    [LoggerMessage(62, LogLevel.Information, "{Type} {Name} recovery buy skipped symbol {Symbol} without any full lot to recover")]
    private partial void LogRecoveryBuySkippedSymbolWithoutFullLot(string type, string name, string symbol);

    [LoggerMessage(63, LogLevel.Information, "{Type} {Name} recovery buy skipped symbol {Symbol} due to inability to identify price for RSI({Periods}) {RSI:F2}")]
    private partial void LogRecoveryBuySkippedSymbolWithUnknownRsiPrice(string type, string name, string symbol, int periods, decimal rsi);

    [LoggerMessage(64, LogLevel.Information, "{Type} {Name} recovery buy skipped symbol {Symbol} on cooldown until {Cooldown}")]
    private partial void LogRecoveryBuySkippedSymbolOnCooldown(string type, string name, string symbol, DateTime cooldown);

    [LoggerMessage(65, LogLevel.Information, "{Type} {Name} recovery buy skipped symbol {Symbol} with recovery buy price {BuyPrice:F8} {Quote} not under drop price of {DropPrice:F8} {Quote}")]
    private partial void LogRecoveryBuySkippedSymbolWithBuyPriceNotUnderDropPrice(string type, string name, string symbol, decimal buyPrice, string quote, decimal dropPrice);

    [LoggerMessage(66, LogLevel.Information, "{Type} {Name} recovery buy skipped symbol {Symbol} with ticker of {Ticker:F8} {Quote} not under buy price of {BuyPrice:F8} {Quote}")]
    private partial void LogRecoveryBuySkippedSymbolWithTickerNotUnderBuyPrice(string type, string name, string symbol, decimal ticker, string quote, decimal buyPrice);

    [LoggerMessage(67, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with highest present value {PV:F8} {Quote}")]
    private partial void LogSymbolWithHighestPresentValue(string type, string name, string symbol, decimal pv, string quote);

    [LoggerMessage(68, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with highest present value above break even {PV:F8} {Quote}")]
    private partial void LogSymbolWithHighestPresentValueAboveBreakEven(string type, string name, string symbol, decimal pv, string quote);

    [LoggerMessage(69, LogLevel.Information, "{Type} {Name} entry buy placing order for {Quantity} {Asset} at {Price} {Quote}")]
    private partial void LogEntryBuyPlacingOrder(string type, string name, decimal quantity, string asset, decimal price, string quote);

    [LoggerMessage(70, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} with buy price of {Price:F8} {Quote} below min percent price of {MinPrice:F8} {Quote}")]
    private partial void LogEntryBuySkippedSymbolWithPriceBelowMinPercent(string type, string name, string symbol, decimal price, decimal minPrice, string quote);

    [LoggerMessage(71, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} with ticker of {Ticker:F8} {Quote} not above range price of {RangePrice:F8} {Quote} for sell price of {SellPrice:F8} {Quote}")]
    private partial void LogRecoverySellSkippedSymbolWithTickerNotAboveRangePrice(string type, string name, string symbol, decimal ticker, string quote, decimal rangePrice, decimal sellPrice);

    [LoggerMessage(72, LogLevel.Information, "{Type} {Name} {Symbol} sell off skipped symbol with a recovery buy under local max of {LocalMax:F8} {Quote}")]
    private partial void LogSellOffSkippedSymbolWithRecoveryBuy(string type, string name, string symbol, decimal localMax, string quote);

    [LoggerMessage(73, LogLevel.Information, "{Type} {Name} {Symbol} recovery sell skipped symbol with RMI of {RMI:F2} under high threshold of {HighRMI:F2}")]
    private partial void LogRecoverySellSkippedSymbolWithRmiUnderHighThreshold(string type, string name, string symbol, decimal rmi, decimal highRmi);

    [LoggerMessage(74, LogLevel.Information, "{Type} {Name} {Symbol} recovery sell cannot calculate price for entry RMI of {RMI:F2}")]
    private partial void LogRecoverySellSkippedSymbolWithUnknownRmiPrice(string type, string name, string symbol, decimal rmi);

    [LoggerMessage(75, LogLevel.Information, "{Type} {Name} {Symbol} sell off skipped symbol with RMI of {RMI:F2} under high threshold of {HighRMI:F2}")]
    private partial void LogSellOffSkippedSymbolWithRmiUnderHighThreshold(string type, string name, string symbol, decimal rmi, decimal highRmi);

    [LoggerMessage(76, LogLevel.Information, "{Type} {Name} {Symbol} sell off cannot calculate price for entry RMI of {RMI:F2}")]
    private partial void LogSellOffSkippedSymbolWithUnknownRmiPrice(string type, string name, string symbol, decimal rmi);

    [LoggerMessage(77, LogLevel.Information, "{Type} {Name} {Symbol} sell skipped symbol with current {OrderType} {OrderSide} at {CurrentStopPrice:F8} {Quote} higher than calculated {StopPrice:F8} {Quote}")]
    private partial void LogSellSkippedSymbolWithHigherStopPrice(string type, string name, string symbol, OrderType orderType, OrderSide orderSide, decimal currentStopPrice, decimal stopPrice, string quote);

    [LoggerMessage(78, LogLevel.Information, "{Type} {Name} {Symbol} buy step skipped symbol on cooldown until {Cooldown}")]
    private partial void LogBuySkippedSymbolOnCooldown(string type, string name, string symbol, DateTime cooldown);

    [LoggerMessage(79, LogLevel.Information, "{Type} {Name} {Symbol} buy step skipped symbol with high current RMI of {RMI:F2}")]
    private partial void LogBuySkippedSymbolWithHighCurrentRmi(string type, string name, string symbol, decimal rmi);

    [LoggerMessage(80, LogLevel.Information, "{Type} {Name} {Symbol} buy step cannot calculate price for entry RMI of {RMI:F2}")]
    private partial void LogBuySkippedSymbolWithUnknownRmiPrice(string type, string name, string symbol, decimal rmi);

    [LoggerMessage(81, LogLevel.Information, "{Type} {Name} {Symbol} sell step skipped symbol with current {OrderType} {OrderSide} at {CurrentStopPrice:F8} {Quote} lower than calculated {StopPrice:F8} {Quote}")]
    private partial void LogSellSkippedSymbolWithLowerStopPrice(string type, string name, string symbol, OrderType orderType, OrderSide orderSide, decimal currentStopPrice, decimal stopPrice, string quote);

    [LoggerMessage(82, LogLevel.Information, "{Type} {Name} {Symbol} sell step skipped symbol with invalid lot step size of {LotStepSize:F8} {Asset}")]
    private partial void LogSkippedSymbolWithInvalidLotStepSize(string type, string name, string symbol, decimal lotStepSize, string asset);

    [LoggerMessage(83, LogLevel.Information, "{Type} {Name} {Symbol} sell step skipped symbol with target stop price {SellPrice:F8} {Quote} at or above ticker {Ticker:F8} {Quote}")]
    private partial void LogSellSkippedSymbolWithHighStopPrice(string type, string name, string symbol, decimal sellPrice, decimal ticker, string quote);

    [LoggerMessage(84, LogLevel.Information, "{Type} {Name} {Symbol} buy step skipped symbol with zero lots on the opening exclusion set")]
    private partial void LogBuySkippedSymbolOnOpeningExclusionSet(string type, string name, string symbol);

    [LoggerMessage(85, LogLevel.Information, "{Type} {Name} {Symbol} buy step skipped symbol with ticker {Ticker:F8} {Quote} below trend SMA({Periods}) {Sma:F8} {Quote}")]
    private partial void LogBuySkippedSymbolWithTickerBelowTrendSma(string type, string name, string symbol, decimal ticker, int periods, decimal sma, string quote);

    [LoggerMessage(86, LogLevel.Information, "{Type} {Name} {Symbol} stop loss sell skipped symbol {Symbol} because selling is disabled")]
    private partial void LogStopLossSellDisabled(string type, string name, string symbol);

    [LoggerMessage(87, LogLevel.Information, "{Type} {Name} {Symbol} stop loss sell skipped symbol {Symbol} without any full lot to sell")]
    private partial void LogStopLossSellSkippedSymbolWithoutFullLot(string type, string name, string symbol);

    [LoggerMessage(88, LogLevel.Information, "{Type} {Name} {Symbol} stop loss sell skipped symbol with PV {PV:F8} {Quote} under min notion {MinNotional:F8} {Quote}")]
    private partial void LogStopLossSellSkippedSymbolWithPvUnderMinNotional(string type, string name, string symbol, decimal pv, decimal minNotional, string quote);

    [LoggerMessage(89, LogLevel.Information, "{Type} {Name} {Symbol} stop loss skipped symbol with quantity {Quantity:F8} {Asset} less than min quantity of {MinQuantity:F8} {Asset}")]
    private partial void LogStopLossSellSkippedSymbolWithQuantityUnderMin(string type, string name, string symbol, decimal quantity, decimal minQuantity, string asset);

    [LoggerMessage(90, LogLevel.Information, "{Type} {Name} {Symbol} stop loss sell skipped symbol with ticker {Ticker:F8} {Quote} above avg price {AvgPrice:F8} {Quote}")]
    private partial void LogStopLossSellSkippedSymbolWithTickerAboveAvgPrice(string type, string name, string symbol, decimal ticker, decimal avgPrice, string quote);

    [LoggerMessage(91, LogLevel.Information, "{Type} {Name} {Symbol} stop loss sell elected symbol for market sell at ticker {Ticker:F8} {Quote}")]
    private partial void LogStopLossSellElectedSymbolForMarketSell(string type, string name, string symbol, decimal ticker, string quote);

    [LoggerMessage(92, LogLevel.Information, "{Type} {Name} reports closed symbol {Symbol} with leftover quantity {Quantity:F8} {Asset} worth {PV:F8} {Quote}")]
    private partial void LogClosedSymbol(string type, string name, string symbol, decimal quantity, string asset, decimal pv, string quote);

    [LoggerMessage(93, LogLevel.Warning, "{Type} {Name} {Symbol} cannot place sell order with quantity of {Quantity:F8} {Asset} under min of {MinQuantity:F8} {Asset}")]
    private partial void LogCannotPlaceSellOrderWithQuantityUnderMin(string type, string name, string symbol, decimal quantity, string asset, decimal minQuantity);

    [LoggerMessage(94, LogLevel.Warning, "{Type} {Name} {Symbol} cannot place sell order with notional of {Notional:F8} {Quote} under min of {MinNotional:F8} {Quote}")]
    private partial void LogCannotPlaceSellOrderWithNotionalUnderMin(string type, string name, string symbol, decimal notional, string quote, decimal minNotional);

    [LoggerMessage(95, LogLevel.Warning, "{Type} {Name} {Symbol} cannot calculate stop loss with current rules")]
    private partial void LogCannotCalculateStopLoss(string type, string name, string symbol);

    #endregion Logging
}