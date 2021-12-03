using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Outcompute.Trader.Trading.Commands;

namespace Outcompute.Trader.Trading.Algorithms.Samples.Portfolio;

public partial class PortfolioAlgo : Algo
{
    private readonly IOptionsMonitor<PortfolioAlgoOptions> _monitor;
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;

    public PortfolioAlgo(IOptionsMonitor<PortfolioAlgoOptions> monitor, ILogger<PortfolioAlgo> logger, ISystemClock clock)
    {
        _monitor = monitor;
        _logger = logger;
        _clock = clock;
    }

    private const string TypeName = nameof(PortfolioAlgo);
    private const string SellOffTag = "SellOff";
    private const string RecoverySellTag = "RecoverySell";
    private const string RecoveryBuyTag = "RecoveryBuy";
    private const string TopUpBuyTag = "TopUpBuy";
    private const string EntryBuyTag = "EntryBuy";

    private PortfolioAlgoOptions _options = null!;

    protected override IAlgoCommand OnExecute()
    {
        // always get the latest options so the user can change them in real-time
        _options = _monitor.Get(Context.Name);

        var command = Noop();
        var lookup = new Dictionary<string, PositionStats>();

        foreach (var item in Context.Data)
        {
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

            if (TryCreateSellOff(item, lots, stats, out var selloff))
            {
                command = command.Then(selloff);
            }
            else
            {
                command = command.Then(CancelSellOff(item.Symbol));
            }

            if (TryCreateRecoverySell(item, lots, out var recoverySell))
            {
                command = command.Then(recoverySell);
            }
            else
            {
                command = command.Then(CancelRecoverySell(item.Symbol));
            }

            if (TryCreateRecoveryBuy(item, lots, out var recoveryBuy))
            {
                command = command.Then(recoveryBuy);
            }
            else
            {
                command = command.Then(CancelRecoveryBuy(item.Symbol));
            }

            if (TryCreateTopUpBuy(item, lots, out var topUpBuy))
            {
                command = command.Then(topUpBuy);
            }
            else
            {
                command = command.Then(CancelTopUpBuy(item.Symbol));
            }

            if (TryCreateEntryBuy(item, lots, out var entryBuy))
            {
                command = command.Then(entryBuy);
            }
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
                (pv - cost) / cost,
                pv - cost,
                rpnl,
                pv - cost + rpnl);

            // every non zero symbol by relative pnl
            foreach (var item in stats
                .Where(x => x.Stats.TotalQuantity > 0)
                .OrderBy(x => x.Stats.RelativePnL))
            {
                LogSymbolAtBreakEven(TypeName, Context.Name, item.Symbol.Name, item.Stats.RelativePnL, item.Stats.AbsolutePnL, item.Symbol.QuoteAsset, item.Stats.PresentValue);
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
                .Where(x => !_options.SellOff.ExcludeSymbols.Contains(x.Symbol.Name))
                .OrderByDescending(x => x.Stats.PresentValue)
                .FirstOrDefault();

            if (!IsNullOrEmpty(highPv.Symbol?.Name))
            {
                LogSymbolWithHighestPresentValue(TypeName, Context.Name, highPv.Symbol.Name, highPv.Stats.PresentValue, highPv.Symbol.QuoteAsset);
            }

            // report on the highest sellable pv above break even
            var highPvBreakEven = stats
                .Where(x => !_options.SellOff.ExcludeSymbols.Contains(x.Symbol.Name))
                .Where(x => x.Stats.TotalQuantity > 0)
                .Where(x => x.Stats.RelativePnL >= 0)
                .OrderByDescending(x => x.Stats.PresentValue)
                .FirstOrDefault();

            if (!IsNullOrEmpty(highPv.Symbol?.Name))
            {
                LogSymbolWithHighestPresentValueAboveBreakEven(TypeName, Context.Name, highPvBreakEven.Symbol.Name, highPvBreakEven.Stats.PresentValue, highPvBreakEven.Symbol.QuoteAsset);
            }
        }
    }

    private bool TryCreateSellOff(SymbolData item, IList<PositionLot> lots, PositionStats stats, out IAlgoCommand command)
    {
        // placeholder
        command = Noop();

        // selling must be enabled
        if (!_options.SellOff.Enabled)
        {
            LogSellOffDisabled(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // symbol must not be on the never sell set
        if (_options.SellOff.ExcludeSymbols.Contains(item.Symbol.Name))
        {
            LogSellOffSkippedSymbolOnExclusionSet(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // there must be enough quantity to sell off
        if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
        {
            LogSellOffSkippedSymbolWithQuantityUnderMinLotSize(TypeName, Context.Name, item.Symbol.Name, stats.TotalQuantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
            return false;
        }

        /*
        // there must be enough notional value to sell off right now
        // todo: this applies at sell price
        if (stats.PresentValue < item.Symbol.Filters.MinNotional.MinNotional)
        {
            LogSellOffSkippedSymbolWithPresentValueUnderMinNotional(TypeName, Context.Name, item.Symbol.Name, stats.PresentValue, item.Symbol.Filters.MinNotional.MinNotional, item.Symbol.QuoteAsset);
            return false;
        }
        */

        // skip if the last lot is a recovery buy and recovery is enabled for this symbol
        if (_options.Recovery.Enabled && !_options.Recovery.ExcludeSymbols.Contains(item.Symbol.Name) && HasLocalMax(lots))
        {
            // todo: log
            return false;
        }

        // calculate the desired sell price based on desired profit
        var profitPrice = _options.SellOff.TriggerRate * stats.AvgPrice;

        // calculate the desired sell price based on desired rsi
        if (!item.Klines.TryGetPriceForRsi(x => x.ClosePrice, _options.SellOff.Rsi.Periods, _options.SellOff.Rsi.Overbought, out var rsiPrice))
        {
            // todo: log
            return false;
        }

        // keep the max of the two
        var price = Math.Max(profitPrice, rsiPrice);
        price = price.AdjustPriceUpToTickSize(item.Symbol);

        // the notional must be sellable at the target price
        var notional = stats.TotalQuantity * price;
        if (notional < item.Symbol.Filters.MinNotional.MinNotional)
        {
            // todo: log
            return false;
        }

        // skip if we have not reached the target price yet
        if (item.Ticker.ClosePrice <= (price * (1 - _options.SellOff.OrderPriceRange)))
        {
            // todo: log
            return false;
        }

        // skip if there is already an open sell off order
        // this means that a previously placed order hit and is being filled
        if (item.Orders.Open.Any(x => x.Type == OrderType.Limit && x.Side == OrderSide.Sell && x.ClientOrderId == SellOffTag))
        {
            // todo: log
            // returning true but not assigning the command
            return true;
        }

        // if all the stars align then dump the assets
        LogSellOffElectedSymbol(TypeName, Context.Name, item.Symbol.Name);

        // take any current sell orders into account for spot balance release
        var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Sell).Sum(x => x.OriginalQuantity);

        command =
            Sequence(
                EnsureSpotBalance(item.Symbol.BaseAsset, Math.Max(stats.TotalQuantity - locked, 0), _options.UseSavings, _options.UseSwapPools),
                EnsureSingleOrder(item.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, stats.TotalQuantity, null, price, null, SellOffTag));

        return true;
    }

    private IAlgoCommand CancelSellOff(Symbol symbol)
    {
        return CancelOpenOrders(symbol, OrderSide.Sell, null, SellOffTag);
    }

    private bool TryCreateEntryBuy(SymbolData item, IList<PositionLot> lots, out IAlgoCommand command)
    {
        command = Noop();

        // entry buying must be enabled
        if (!_options.EntryBuy.Enabled)
        {
            LogEntryBuyDisabled(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // only look at symbols without a full lot
        if (lots.Count > 0)
        {
            LogEntryBuySkippedSymbolWithLots(TypeName, Context.Name, item.Symbol.Name, lots.Count, lots.Sum(x => x.Quantity), item.Symbol.BaseAsset);
            return false;
        }

        // identify the entry price
        if (!item.Klines.TryGetPriceForRsi(x => x.ClosePrice, _options.EntryBuy.Rsi.Periods, _options.EntryBuy.Rsi.Oversold, out var price))
        {
            LogEntryBuySkippedSymbolWithUnknownRsiPrice(TypeName, Context.Name, item.Symbol.Name, _options.EntryBuy.Rsi.Periods, _options.EntryBuy.Rsi.Oversold);
            return false;
        }
        price = price.AdjustPriceDownToTickSize(item.Symbol);

        // price must above the price filter
        // todo: push this check to the command executors
        var minPrice = item.Ticker.ClosePrice * item.Symbol.Filters.PercentPrice.MultiplierDown;
        if (price < minPrice)
        {
            LogEntryBuySkippedSymbolWithPriceBelowMinPercent(TypeName, Context.Name, item.Symbol.Name, price, minPrice, item.Symbol.QuoteAsset);
            return false;
        }

        // identify the appropriate buy quantity for this price
        var quantity = CalculateBuyQuantity(item, price, _options.EntryBuy.BalanceRate);

        // create the limit order
        LogEntryBuyPlacingOrder(TypeName, Context.Name, quantity, item.Symbol.BaseAsset, price, item.Symbol.QuoteAsset);

        // take any current buy orders into account for spot balance release
        var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Buy).Sum(x => x.OriginalQuantity * x.Price);

        command = Sequence(
            EnsureSpotBalance(item.Symbol.QuoteAsset, Math.Max((quantity * price) - locked, 0), _options.UseSavings, _options.UseSwapPools),
            EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, null, price, null, EntryBuyTag));

        return true;
    }

    private bool TryCreateTopUpBuy(SymbolData item, IList<PositionLot> lots, out IAlgoCommand command)
    {
        command = Noop();

        // topping up must be enabled
        if (!_options.TopUpBuy.Enabled)
        {
            LogTopUpDisabled(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // there must be something to top up
        if (lots.Count == 0)
        {
            LogTopUpSkippedSymbolWithoutFullLot(TypeName, Context.Name, item.Symbol.Name, item.Symbol.Filters.LotSize.StepSize, item.Symbol.BaseAsset);
            return false;
        }

        // the symbol must not be overbought
        if (_options.TopUpBuy.Rsi.Enabled)
        {
            var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.TopUpBuy.Rsi.Periods);
            if (rsi >= _options.TopUpBuy.Rsi.Overbought)
            {
                LogTopUpSkippedSymbolWithOverboughtRsi(TypeName, Context.Name, item.Symbol.Name, _options.TopUpBuy.Rsi.Periods, rsi, _options.TopUpBuy.Rsi.Overbought);
                return false;
            }
        }

        // the symbol must be under the safety sma
        if (_options.TopUpBuy.Sma.Enabled)
        {
            var sma = item.Klines.LastSma(x => x.ClosePrice, _options.TopUpBuy.Sma.Periods);
            if (item.Ticker.ClosePrice >= sma)
            {
                LogTopUpSkippedSymbolWithTickerAboveSafetySma(TypeName, Context.Name, item.Symbol.Name, item.Ticker.ClosePrice, item.Symbol.BaseAsset, _options.TopUpBuy.Sma.Periods, sma);
                return false;
            }
        }

        // only top up if the last lot is not a recovery buy - if so the last lot will be lower than a local max
        // this rule only applies to symbols that participate in recovery
        if (_options.Recovery.Enabled && !_options.Recovery.ExcludeSymbols.Contains(item.Symbol.Name) && HasLocalMax(lots))
        {
            LogTopUpSkippedSymbolWithRecoveryBuy(TypeName, Context.Name, item.Symbol.Name, item.AutoPosition.Positions.Last.Quantity, item.Symbol.BaseAsset, item.AutoPosition.Positions.Last.Price, item.Symbol.QuoteAsset);
            return false;
        }

        // skip symbols below min required for top up
        var lastLot = lots[0];
        var price = lastLot.AvgPrice * (1 + _options.TopUpBuy.RaiseRate);
        price = price.AdjustPriceUpToTickSize(item.Symbol);

        if (item.Ticker.ClosePrice < price)
        {
            LogTopUpSkippedSymbolWithPriceNotHighEnough(TypeName, Context.Name, item.Symbol.Name, item.Ticker.ClosePrice, price, _options.TopUpBuy.RaiseRate, item.AutoPosition.Positions.Last.Price);
            return false;
        }

        // raise to the current ticker if needed for the order to go through
        price = Math.Max(item.Ticker.ClosePrice, price);

        // if we got here then we have a top up
        LogTopUpElectedSymbol(TypeName, Context.Name, item.Name, item.Ticker.ClosePrice, item.Symbol.QuoteAsset);

        // identify the appropriate buy quantity for this price
        var quantity = CalculateBuyQuantity(item, price, _options.TopUpBuy.BalanceRate);

        // skip if there is already an open order at an equal or higher ticker to avoid order twitching
        if (item.Orders.Open.Any(x => x.Side == OrderSide.Buy && x.Type == OrderType.Limit && x.Price >= price))
        {
            // todo: log
            return false;
        }

        // create the limit order
        // todo: log

        // take any current buy orders into account for spot balance release
        var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Buy).Sum(x => x.OriginalQuantity * x.Price);

        command = Sequence(
            EnsureSpotBalance(item.Symbol.QuoteAsset, Math.Max((quantity * price) - locked, 0), _options.UseSavings, _options.UseSwapPools),
            EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, null, price, null, TopUpBuyTag));

        return true;
    }

    private IAlgoCommand CancelTopUpBuy(Symbol symbol)
    {
        return CancelOpenOrders(symbol, OrderSide.Buy, null, TopUpBuyTag);
    }

    private bool TryCreateRecoveryBuy(SymbolData item, IList<PositionLot> lots, out IAlgoCommand command)
    {
        // placeholder
        command = Noop();

        // recovery must be enabled
        if (!_options.Recovery.Enabled)
        {
            LogBuyRecoveryDisabled(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // symbol must not be on the exclusion set
        if (_options.Recovery.ExcludeSymbols.Contains(item.Symbol.Name))
        {
            LogRecoveryBuySkippedSymbolOnExclusionSet(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // there must something to recover
        if (lots.Count == 0)
        {
            LogRecoveryBuySkippedSymbolWithoutFullLot(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // calculate the price required for the recovery rsi
        if (!item.Klines.TryGetPriceForRsi(x => x.ClosePrice, _options.Recovery.Rsi.Periods, _options.Recovery.Rsi.Buy, out var buyPrice))
        {
            LogRecoveryBuySkippedSymbolWithUnknownRsiPrice(TypeName, Context.Name, item.Symbol.Name, _options.Recovery.Rsi.Periods, _options.Recovery.Rsi.Buy);
            return false;
        }
        buyPrice = buyPrice.AdjustPriceDownToTickSize(item.Symbol);

        // enough time must have passed since the last buy
        var lastLot = lots[0];
        var cooldown = lastLot.Time.Add(_options.Recovery.Cooldown);
        if (cooldown >= _clock.UtcNow)
        {
            LogRecoveryBuySkippedSymbolOnCooldown(TypeName, Context.Name, item.Symbol.Name, cooldown);
            return false;
        }

        // the buy price must be low enough from the last buy for a recovery buy to take place
        var dropPrice = lastLot.AvgPrice * (1 - _options.Recovery.DropRate);
        if (buyPrice >= dropPrice)
        {
            LogRecoveryBuySkippedSymbolWithBuyPriceNotUnderDropPrice(TypeName, Context.Name, item.Symbol.Name, buyPrice, item.Symbol.QuoteAsset, dropPrice);
            return false;
        }

        // ticker must be within range of the buy price to bother reserving funds
        if (item.Ticker.ClosePrice >= buyPrice * (1 + _options.Recovery.BuyOrderPriceRange))
        {
            LogRecoveryBuySkippedSymbolWithTickerNotUnderBuyPrice(TypeName, Context.Name, item.Symbol.Name, item.Ticker.ClosePrice, item.Symbol.QuoteAsset, buyPrice);
            return false;
        }

        // calculate the quantity
        var quantity = CalculateBuyQuantity(item, buyPrice, _options.Recovery.BalanceRate);

        LogRecoveryBuyPlacingOrder(TypeName, Context.Name, OrderType.Limit, OrderSide.Buy, quantity, buyPrice, item.Symbol.BaseAsset, item.Symbol.QuoteAsset);

        // take any current buy orders into account for spot balance release
        var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Buy).Sum(x => x.OriginalQuantity * x.Price);

        command = Sequence(
            EnsureSpotBalance(item.Symbol.QuoteAsset, Math.Max((quantity * buyPrice) - locked, 0), _options.UseSavings, _options.UseSwapPools),
            EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, null, buyPrice, null, RecoveryBuyTag));

        return true;
    }

    private IAlgoCommand CancelRecoveryBuy(Symbol symbol)
    {
        return CancelOpenOrders(symbol, OrderSide.Buy, null, RecoveryBuyTag);
    }

    private bool TryCreateRecoverySell(SymbolData item, IList<PositionLot> lots, out IAlgoCommand command)
    {
        // placeholder
        command = Noop();

        // recovery must be enabled
        if (!_options.Recovery.Enabled)
        {
            LogRecoverySellDisabled(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // symbol must not be on the exclusion set
        if (_options.Recovery.ExcludeSymbols.Contains(item.Symbol.Name))
        {
            LogRecoverySellSkippedSymbolOnExclusionSet(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // there must be something to recover
        if (lots.Count == 0)
        {
            LogRecoverySellSkippedSymbolWithoutFullLot(TypeName, Context.Name, item.Symbol.Name);
            return false;
        }

        // identify the recovery sell price for the target rsi
        if (!item.Klines.TryGetPriceForRsi(x => x.ClosePrice, _options.Recovery.Rsi.Periods, _options.Recovery.Rsi.Sell, out var sellPrice))
        {
            LogRecoverySellSkippedSymbolWithUnknownRsiPrice(TypeName, Context.Name, item.Symbol.Name, _options.Recovery.Rsi.Periods, _options.Recovery.Rsi.Sell);
            return false;
        }
        sellPrice = sellPrice.AdjustPriceUpToTickSize(item.Symbol);

        // the last lot must be under the recovery sell price
        var lastLot = lots[0];
        if (lastLot.AvgPrice >= sellPrice)
        {
            LogRecoverySellSkippedSymbolWithHighLastLotPrice(TypeName, Context.Name, item.Symbol.Name, lastLot.AvgPrice, item.Symbol.QuoteAsset, sellPrice);
            return false;
        }

        // there must be a local max from the last lot for it to classify as a recovery buy
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
        if (maxPrice <= lastLot.AvgPrice)
        {
            LogRecoverySellSkippedSymbolWithoutLocalMax(TypeName, Context.Name, item.Symbol.Name, lastLot.AvgPrice, item.Symbol.QuoteAsset);
            return false;
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

        if (electedQuantity <= 0)
        {
            LogRecoverySellSkippedSymbolWithLotsNotUnderMaxPrice(TypeName, Context.Name, item.Symbol.Name, maxPrice, item.Symbol.QuoteAsset);
            return false;
        }

        // if we found something to sell then place the recovery sell
        LogRecoverySellElectedSymbol(TypeName, Context.Name, item.Symbol.Name, electedQuantity, item.Symbol.BaseAsset, sellPrice, item.Symbol.QuoteAsset);

        // take any current sell orders into account for spot balance release
        var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Sell).Sum(x => x.OriginalQuantity);

        command = Sequence(
            EnsureSpotBalance(item.Symbol.BaseAsset, Math.Max(electedQuantity - locked, 0), _options.UseSavings, _options.UseSwapPools),
            EnsureSingleOrder(item.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, electedQuantity, null, sellPrice, null, RecoverySellTag));

        return true;
    }

    private IAlgoCommand CancelRecoverySell(Symbol symbol)
    {
        return CancelOpenOrders(symbol, OrderSide.Sell, null, RecoverySellTag);
    }

    private bool HasLocalMax(IList<PositionLot> lots)
    {
        return TryFindLocalMax(lots, out _);
    }

    private bool TryFindLocalMax(IList<PositionLot> lots, out decimal max)
    {
        max = 0;

        if (lots.Count == 0)
        {
            return false;
        }

        foreach (var lot in lots)
        {
            if (lot.AvgPrice >= max)
            {
                max = lot.AvgPrice;
            }
            else
            {
                break;
            }
        }

        return max > lots[0].AvgPrice;
    }

    private decimal CalculateBuyQuantity(SymbolData item, decimal price, decimal balanceRate)
    {
        // calculate the notional to use for buying
        var notional = item.Spot.QuoteAsset.Free
            + (_options.UseSavings ? item.Savings.QuoteAsset.FreeAmount : 0)
            + (_options.UseSwapPools ? item.SwapPools.QuoteAsset.Total : 0);

        notional *= balanceRate;

        // raise to a valid number
        notional = notional.AdjustTotalUpToMinNotional(item.Symbol);
        notional = notional.AdjustPriceUpToTickSize(item.Symbol);

        // pad the order with the fee
        notional *= 2 + _options.FeeRate;

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

    [LoggerMessage(44, LogLevel.Information, "{Type} {Name} recovery sell elected symbol {Symbol} for selling at {Quantity:F8} {Asset} at {SellPrice:F8} {Quote}")]
    private partial void LogRecoverySellElectedSymbol(string type, string name, string symbol, decimal quantity, string asset, decimal sellPrice, string quote);

    [LoggerMessage(45, LogLevel.Information, "{Type} {Name} top up skipped symbol {Symbol} with a recovery buy of {Quantity:F8} {Asset} at {Price:F8} {Quote}")]
    private partial void LogTopUpSkippedSymbolWithRecoveryBuy(string type, string name, string symbol, decimal quantity, string asset, decimal price, string quote);

    [LoggerMessage(46, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} above break even with (PnL: {UnrealizedPnl:P2}, Unrealized: {UnrealizedAbsPnl:F8} {Quote}, PV: {PV:F8} {Quote}")]
    private partial void LogSymbolAtBreakEven(string type, string name, string symbol, decimal unrealizedPnl, decimal unrealizedAbsPnl, string quote, decimal pv);

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

    [LoggerMessage(52, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} without any full lot to recover")]
    private partial void LogRecoverySellSkippedSymbolWithoutFullLot(string type, string name, string symbol);

    [LoggerMessage(53, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} due to inability to identify price for RSI({Periods}) {RSI:F2}")]
    private partial void LogRecoverySellSkippedSymbolWithUnknownRsiPrice(string type, string name, string symbol, int periods, decimal rsi);

    [LoggerMessage(54, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} with last lot price of {LastLotPrice:F8} {Quote} not under the recovery sell price of {SellPrice:F8} {Quote}")]
    private partial void LogRecoverySellSkippedSymbolWithHighLastLotPrice(string type, string name, string symbol, decimal lastLotPrice, string quote, decimal sellPrice);

    [LoggerMessage(55, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} because last lot price of {LastLotPrice:F8} {Quote} does not have a local max")]
    private partial void LogRecoverySellSkippedSymbolWithoutLocalMax(string type, string name, string symbol, decimal lastLotPrice, string quote);

    [LoggerMessage(56, LogLevel.Information, "{Type} {Name} recovery sell skipped symbol {Symbol} because it could not fit any lot under the local max price of {MaxPrice:F8} {Quote}")]
    private partial void LogRecoverySellSkippedSymbolWithLotsNotUnderMaxPrice(string type, string name, string symbol, decimal maxPrice, string quote);

    [LoggerMessage(57, LogLevel.Information, "{Type} {Name} entry buy skipped symbol {Symbol} because entry buying is disabled")]
    private partial void LogEntryBuyDisabled(string type, string name, string symbol);

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

    #endregion Logging
}