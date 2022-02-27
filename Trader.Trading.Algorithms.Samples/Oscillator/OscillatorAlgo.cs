using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Outcompute.Trader.Trading.Commands;
using static System.Collections.Generic.KdjExtensions;

namespace Outcompute.Trader.Trading.Algorithms.Samples.Oscillator
{
    internal partial class OscillatorAlgo : Algo
    {
        private readonly OscillatorAlgoOptions _options;
        private readonly ILogger _logger;

        public OscillatorAlgo(IOptionsSnapshot<OscillatorAlgoOptions> options, ILogger<OscillatorAlgo> logger)
        {
            _options = options.Get(Context.Name);
            _logger = logger;
        }

        private const string TypeName = nameof(OscillatorAlgo);
        private const string EntryBuyTag = "EntryBuy";
        private const string ExitSellTag = "ExitSell";

        protected override IAlgoCommand OnExecute()
        {
            var result = Noop();
            var lookup = new Dictionary<string, PositionStats>();

            foreach (var item in Context.Data)
            {
                // skip invalidated symbol
                if (!item.IsValid)
                {
                    LogSkippedInvalidatedSymbol(TypeName, Context.Name, item.Symbol.Name);
                    continue;
                }

                // get pv stats over sellable positions from the last
                var lots = item.AutoPosition.Positions.Reverse().EnumerateLots(item.Symbol.Filters.LotSize.StepSize).Reverse().ToList();
                var stats = lots.GetStats(item.Ticker.ClosePrice);

                result = result.Then(TryEnter(item, lots, stats));

                if (TryExit(item, lots, stats, out var exit))
                {
                    result = result.Then(exit);
                }

                // cache for the reporting method
                lookup[item.Symbol.Name] = stats;
            }

            ReportAggregateStats(lookup);

            return result;
        }

        private decimal CalculateBuyQuantity(SymbolData item, PositionStats stats, decimal price)
        {
            // calculate the notional to use for buying
            var notional = item.Symbol.RaiseTotalUpToMinNotional(_options.Notional);

            // top up with past realized profits for the first buy only
            if (_options.UseProfits && stats.TotalQuantity == 0)
            {
                notional += item.AutoPosition.ProfitEvents.Sum(x => x.Profit);
                notional -= item.AutoPosition.CommissionEvents.Where(x => x.Asset == item.Symbol.QuoteAsset).Sum(x => x.Commission);
            }

            // raise to a valid number
            notional = item.Symbol.RaiseTotalUpToMinNotional(notional);
            notional = item.Symbol.RaisePriceToTickSize(notional);

            // calculate the quantity for the limit order
            var quantity = notional / price;

            // raise the quantity to a valid number
            quantity = item.Symbol.RaiseQuantityToMinLotSize(quantity);
            quantity = item.Symbol.RaiseQuantityToLotStepSize(quantity);

            return quantity;
        }

        private IAlgoCommand TryEnter(SymbolData item, IList<PositionLot> lots, PositionStats stats)
        {
            IAlgoCommand Clear() => CancelOpenOrders(item.Symbol, OrderSide.Buy);

            // skip if the symbol is on buy cooldown
            if (lots.Count > 0 && lots[^1].Time.AddHours(1) >= Context.TickTime)
            {
                return Clear();
            }

            // skip if opening disabled
            if (lots.Count == 0 && _options.ExcludeFromOpening.Contains(item.Symbol.Name))
            {
                LogSymbolClosed(TypeName, Context.Name, item.Symbol.Name);
                return Clear();
            }

            var window = 0.01M;
            var stopPrice = decimal.MaxValue;

            // get the trix diff
            var trix = item.Klines.SkipLast(1).Trix().TakeLast(2).ToList();
            var trixUp = trix[0].Ema3 < trix[1].Ema3;

            // gather stats for entry point decision
            var oversold = item.Klines.SkipLast(1).Kdj().Reverse().TakeWhile(x => x.Side == KdjSide.Down).Any(x => x.J <= 30);

            // clear any open orders if we dont have a setup
            if (!(oversold && trixUp))
            {
                return Clear();
            }

            // predict the next kdj cross from oversold
            if (item.Klines.SkipLast(1).TryGetKdjForUpcross(item.Klines[^1], out var cross))
            {
                var targetBuy = item.Symbol.LowerPriceToTickSize(cross.Price);
                var targetStop = item.Symbol.LowerPriceToTickSize(targetBuy * (1 - window));

                // guard - cross must happen on the lower band
                if (cross.K >= 50)
                {
                    return Clear();
                }

                if (item.Ticker.ClosePrice < targetStop)
                {
                    stopPrice = Math.Min(stopPrice, targetStop);
                }
            }

            // ignore if we cant predict yet - the price may be jumping around the target
            if (stopPrice == decimal.MaxValue)
            {
                return Noop();
            }

            // skip if there is already an open buy order open at the same stop price
            if (item.Orders.Open.Any(x => x.Side == OrderSide.Buy && x.StopPrice == stopPrice))
            {
                return Noop();
            }

            // calculate the buy price window from the stop
            //var buyPrice = item.Symbol.LowerPriceToTickSize(stopPrice * (1 + window));
            var buyPrice = stopPrice;

            // define the quantity to buy
            var quantity = CalculateBuyQuantity(item, stats, stopPrice);

            return Sequence(
                EnsureSpotBalance(item.Symbol.QuoteAsset, item.Symbol.RaisePriceToTickSize(quantity * buyPrice), true, true),
                EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.StopLossLimit, TimeInForce.GoodTillCanceled, quantity, null, buyPrice, stopPrice, EntryBuyTag));
        }

        private bool TryExit(SymbolData item, IReadOnlyList<PositionLot> lots, PositionStats stats, out IAlgoCommand command)
        {
            command = Noop();

            // there must be something to sell
            if (stats.TotalQuantity == 0)
            {
                return false;
            }

            var stopPrice = 0M;

            // keep the last buy price for reference
            var last = lots.Count > 0 ? lots[^1].AvgPrice : 0M;

            // this will be the sell price window
            var sellWindow = 0.01M;

            // calculate the kdj history for reuse
            var kdj = item.Klines.SkipLast(1).Kdj().ToList();

            // calculate the previous kdj exit for comparison
            KdjValue? lastExit = null;
            for (var i = kdj.Count - 1; i >= 0; i--)
            {
                // look for the last cross down
                if (kdj[i].Cross != KdjCross.Down)
                {
                    continue;
                }

                // ensure this was an overbought cross down
                for (var j = i - 1; j >= 0; j--)
                {
                    // stop scanning if we went past the cross up
                    if (kdj[j].Side == KdjSide.Down)
                    {
                        break;
                    }

                    // identify the exit price for this overbought cross down
                    if (kdj[j].J >= 80)
                    {
                        lastExit = kdj[i];
                        break;
                    }
                }

                if (lastExit.HasValue)
                {
                    break;
                }
            }

            // calculate the tracking stop for reuse
            var trackingStop = item.Symbol.RaisePriceToTickSize(item.Ticker.ClosePrice * (1 - sellWindow));

            // guard - rise to the tracking stop
            if (item.Ticker.ClosePrice > trackingStop)
            {
                stopPrice = Math.Max(stopPrice, trackingStop);
            }

            // guard - raise to a break even when safe
            var safe = Math.Min(lots[^1].AvgPrice, stats.AvgPrice);
            var breakEvenPrice = item.Symbol.RaisePriceToTickSize(safe);
            var breakEvenStop = item.Symbol.RaisePriceToTickSize(breakEvenPrice / (1 - sellWindow));
            var breakEvenTrigger = item.Symbol.RaisePriceToTickSize(breakEvenStop / (1 - sellWindow));
            if (item.Ticker.ClosePrice >= breakEvenTrigger)
            {
                stopPrice = Math.Max(stopPrice, breakEvenStop);
            }

            // raise to a fixed target
            var minimum = lots.Min(x => x.AvgPrice);
            var targetPrice = item.Symbol.RaisePriceToTickSize(minimum * 1.10M);
            var targetStop = item.Symbol.RaisePriceToTickSize(targetPrice / (1 - sellWindow));
            var targetTrigger = item.Symbol.RaisePriceToTickSize(targetStop / (1 - sellWindow));
            if (item.Ticker.ClosePrice >= targetTrigger)
            {
                stopPrice = Math.Max(stopPrice, trackingStop);
            }

            /*
            // guard - raise to the super trend
            var trend = item.Klines.SkipLast(1).SuperTrend().Last();
            var trendStop = item.Symbol.RaisePriceToTickSize(trend.Low);
            stopPrice = Math.Max(stopPrice, trendStop);
            */

            // guard - raise to the sar is possible
            /*
            var sar = item.Klines.ParabolicStopAndReverse().Last();
            if (sar.Direction == PsarDirection.Long)
            {
                var target = item.Symbol.RaisePriceToTickSize(sar.Value);
                stopPrice = Math.Max(stopPrice, target);
            }
            */

            // take - raise to a take profit for atr outliers
            /*
            var atrOutlierPrice = item.Symbol.RaisePriceToTickSize(lots[^1].AvgPrice + (atr * 6));
            var atrOutlierStop = item.Symbol.RaisePriceToTickSize(atrOutlierPrice / (1 - sellWindow));
            var atrOutlierTrigger = item.Symbol.RaisePriceToTickSize(atrOutlierStop / (1 - sellWindow));
            if (item.Ticker.ClosePrice >= atrOutlierTrigger && trackingStop >= atrOutlierStop)
            {
                stopPrice = Math.Max(stopPrice, trackingStop);
            }
            */

            // take - raise to a take profit for bollinger outliers
            var boll = item.Klines.BollingerBands().Last();
            var bollOutlierStop = item.Symbol.RaisePriceToTickSize(boll.High);
            if (item.Ticker.ClosePrice > bollOutlierStop)
            {
                stopPrice = Math.Max(stopPrice, trackingStop);
            }

            // detect the overbough wave
            var overbought = item.Klines.SkipLast(1).Kdj().Reverse().TakeWhile(x => x.Side == KdjSide.Up).Any(x => x.J >= 80);

            // predict the next kdj cross from overbought
            if (overbought && item.Klines.SkipLast(1).TryGetKdjForDowncross(item.Klines[^1], out var cross))
            {
                var kdjPrice = item.Symbol.RaisePriceToTickSize(cross.Price);
                var kdjStop = item.Symbol.RaisePriceToTickSize(kdjPrice / (1 - sellWindow));

                if (item.Ticker.ClosePrice > kdjStop)
                {
                    stopPrice = Math.Max(stopPrice, kdjStop);
                }
            }

            if (stopPrice == 0M)
            {
                return false;
            }

            // skip if there is already an order at a higher price
            if (item.Orders.Open.Any(x => x.Side == OrderSide.Sell && x.StopPrice >= stopPrice))
            {
                return false;
            }

            // calculate sell window
            var sellPrice = item.Symbol.LowerPriceToTickSize(stopPrice * (1 - sellWindow));

            // attempt to average out if possible
            if (!TryGetElectedQuantity(item, lots, sellPrice, out var quantity))
            {
                //quantity = stats.TotalQuantity;
                return false;
            }

            // take any current sell orders into account for spot balance release
            var locked = item.Orders.Open.Where(x => x.Side == OrderSide.Sell).Sum(x => x.OriginalQuantity);
            var required = Math.Max(quantity - locked, 0);
            var notional = item.Symbol.LowerPriceToTickSize(quantity * sellPrice);

            if (quantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                LogCannotPlaceSellOrderWithQuantityUnderMin(TypeName, Context.Name, item.Symbol.Name, quantity, item.Symbol.BaseAsset, item.Symbol.Filters.LotSize.MinQuantity);
                return false;
            }

            if (notional < item.Symbol.Filters.MinNotional.MinNotional)
            {
                LogCannotPlaceSellOrderWithNotionalUnderMin(TypeName, Context.Name, item.Symbol.Name, notional, item.Symbol.QuoteAsset, item.Symbol.Filters.MinNotional.MinNotional);
                return false;
            }

            // place the exit order now
            command = Sequence(
                EnsureSpotBalance(item.Symbol.BaseAsset, required, true, true),
                EnsureSingleOrder(item.Symbol, OrderSide.Sell, OrderType.StopLossLimit, TimeInForce.GoodTillCanceled, quantity, null, sellPrice, stopPrice, ExitSellTag));

            return true;
        }

        private static bool TryGetElectedQuantity(SymbolData item, IReadOnlyList<PositionLot> lots, decimal sellPrice, out decimal electedQuantity)
        {
            // gather all the lots that fit under the sell price
            var quantity = 0M;
            var buyNotional = 0M;
            var sellNotional = 0M;

            electedQuantity = 0M;

            foreach (var lot in lots.Reverse())
            {
                // keep adding everything up so we get a average from the end
                quantity += lot.Quantity;
                buyNotional += lot.Quantity * lot.AvgPrice;
                sellNotional += lot.Quantity * sellPrice;

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

                LogAssetInfo(
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
                /*
                foreach (var item in stats
                    .Where(x => x.Stats.TotalQuantity < x.Symbol.Filters.LotSize.MinQuantity)
                    .Where(x => _options.Buying.Opening.ExcludeSymbols.Contains(x.Symbol.Name)))
                {
                    LogClosedSymbol(TypeName, Context.Name, item.Symbol.Name, item.Stats.TotalQuantity, item.Symbol.BaseAsset, item.Stats.PresentValue, item.Symbol.QuoteAsset);
                }
                */

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
                /*
                var highPv = stats
                    .Where(x => x.Stats.TotalQuantity > 0)
                    .Where(x => !_options.Selling.ExcludeSymbols.Contains(x.Symbol.Name))
                    .OrderByDescending(x => x.Stats.PresentValue)
                    .FirstOrDefault();

                if (!IsNullOrEmpty(highPv.Symbol?.Name))
                {
                    LogSymbolWithHighestPresentValue(TypeName, Context.Name, highPv.Symbol.Name, highPv.Stats.PresentValue, highPv.Symbol.QuoteAsset);
                }
                */

                // report on sellable pv
                /*
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
                */
            }
        }

        #region Logging

        [LoggerMessage(1, LogLevel.Error, "{Type} {Name} skipped invalidated symbol {Symbol}")]
        private partial void LogSkippedInvalidatedSymbol(string type, string name, string symbol);

        [LoggerMessage(2, LogLevel.Information, "{Type} {Name} {Symbol} entry skipped symbol on cooldown until {Cooldown}")]
        private partial void LogEntrySkippedSymbolOnCooldown(string type, string name, string symbol, DateTime cooldown);

        [LoggerMessage(3, LogLevel.Information, "{Type} {Name} {Symbol} entry skipped symbol with low current RMI of {RMI:F2}")]
        private partial void LogEntrySkippedSymbolWithLowCurrentRmi(string type, string name, string symbol, decimal rmi);

        [LoggerMessage(4, LogLevel.Information, "{Type} {Name} {Symbol} entry skipped symbol with high previous RMI of {RMI:F2}")]
        private partial void LogEntrySkippedSymbolWithHighPrevRmi(string type, string name, string symbol, decimal rmi);

        [LoggerMessage(5, LogLevel.Information, "{Type} {Name} {Symbol} entry skipped symbol with high current RMI of {RMI:F2}")]
        private partial void LogEntrySkippedSymbolWithHighCurrentRmi(string type, string name, string symbol, decimal rmi);

        [LoggerMessage(6, LogLevel.Information, "{Type} {Name} {Symbol} entry cannot calculate price for entry RMI of {RMI:F2}")]
        private partial void LogEntrySkippedSymbolWithUnknownRmiPrice(string type, string name, string symbol, decimal rmi);

        [LoggerMessage(7, LogLevel.Information, "{Type} {Name} {Symbol} exit skipped symbol with quantity of {Quantity:F8} {Asset} under min lot size of {MinLotSize:F8} {Asset}")]
        private partial void LogExitSkippedSymbolWithQuantityUnderMinLotSize(string type, string name, string symbol, decimal quantity, decimal minLotSize, string asset);

        [LoggerMessage(8, LogLevel.Information, "{Type} {Name} {Symbol} exist skipped symbol with RMI of {RMI:F2} under high threshold of {HighRMI:F2}")]
        private partial void LogExitSkippedSymbolWithRmiUnderHighThreshold(string type, string name, string symbol, decimal rmi, decimal highRmi);

        [LoggerMessage(9, LogLevel.Information, "{Type} {Name} {Symbol} exit skipped symbol with unsellable notional {Notional:F8} {Quote} from quantity {Quantity} {Asset} at price {Price} {Quote}")]
        private partial void LogExitSkippedSymbolWithUnsellableNotional(string type, string name, string symbol, decimal notional, decimal quantity, decimal price, string asset, string quote);

        [LoggerMessage(10, LogLevel.Information, "{Type} {Name} {Symbol} exit cannot calculate price for entry RMI of {RMI:F2}")]
        private partial void LogExitSkippedSymbolWithUnknownRmiPrice(string type, string name, string symbol, decimal rmi);

        [LoggerMessage(11, LogLevel.Information, "{Type} {Name} {Symbol} exit skipped symbol without lots under the exit price of {ExitPrice:F8} {Quote}")]
        private partial void LogExitSkippedSymbolWithoutLotsUnderExitPrice(string type, string name, string symbol, decimal exitPrice, string quote);

        [LoggerMessage(12, LogLevel.Warning, "{Type} {Name} {Symbol} cannot place sell order with quantity of {Quantity:F8} {Asset} under min of {MinQuantity:F8} {Asset}")]
        private partial void LogCannotPlaceSellOrderWithQuantityUnderMin(string type, string name, string symbol, decimal quantity, string asset, decimal minQuantity);

        [LoggerMessage(13, LogLevel.Warning, "{Type} {Name} {Symbol} cannot place sell order with notional of {Notional:F8} {Quote} under min of {MinNotional:F8} {Quote}")]
        private partial void LogCannotPlaceSellOrderWithNotionalUnderMin(string type, string name, string symbol, decimal notional, string quote, decimal minNotional);

        [LoggerMessage(14, LogLevel.Information, "{Type} {Name} reports {Quote} asset info (U-Cost: {UCost:F8}, U-PV: {UPV:F8}: U-RPnL: {URPNL:P2}, U-AbsPnL: {UAPNL:F8}, R-AbsPnL: {RAPNL:F8}, T-AbsPnL:{TAPNL:F8})")]
        private partial void LogAssetInfo(string type, string name, string quote, decimal ucost, decimal upv, decimal urpnl, decimal uapnl, decimal rapnl, decimal tapnl);

        [LoggerMessage(15, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with (PnL: {UnrealizedPnl:P2}, Unrealized: {UnrealizedAbsPnl:F8} {Quote}, PV: {PV:F8} {Quote}")]
        private partial void LogSymbolPv(string type, string name, string symbol, decimal unrealizedPnl, decimal unrealizedAbsPnl, string quote, decimal pv);

        [LoggerMessage(16, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with lowest unrealized absolute pnl {UnrealizedAbsolutePnl:F8}")]
        private partial void LogSymbolWithLowestAbsolutePnl(string type, string name, string symbol, decimal unrealizedAbsolutePnl);

        [LoggerMessage(17, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with lowest unrealized relative pnl {UnrealizedRelativePnl:P2}")]
        private partial void LogSymbolWithLowestRelativePnl(string type, string name, string symbol, decimal unrealizedRelativePnl);

        [LoggerMessage(18, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with highest unrealized absolute pnl {UnrealizedAbsolutePnl:F8}")]
        private partial void LogSymbolWithHighestAbsolutePnl(string type, string name, string symbol, decimal unrealizedAbsolutePnl);

        [LoggerMessage(19, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} with highest unrealized relative pnl {UnrealizedRelativePnl:P2}")]
        private partial void LogSymbolWithHighestRelativePnl(string type, string name, string symbol, decimal unrealizedRelativePnl);

        [LoggerMessage(20, LogLevel.Information, "{Type} {Name} reports symbol {Symbol} trading is now closed")]
        private partial void LogSymbolClosed(string type, string name, string symbol);

        #endregion Logging
    }
}