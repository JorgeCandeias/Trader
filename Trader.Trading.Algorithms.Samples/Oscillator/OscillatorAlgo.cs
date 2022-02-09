using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Outcompute.Trader.Trading.Commands;

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

                if (TryEnter(item, lots, out var entry))
                {
                    result = result.Then(entry);
                }

                if (TryExit(item, lots, stats, out var exit))
                {
                    result = result.Then(exit);
                }
            }

            return result;
        }

        private decimal CalculateBuyQuantity(SymbolData item, decimal price)
        {
            // calculate the notional to use for buying
            var notional = item.Symbol.RaiseTotalUpToMinNotional(_options.Notional);

            // top up with past realized profits
            if (_options.UseProfits)
            {
                notional += item.AutoPosition.ProfitEvents.Sum(x => x.Profit);
                notional -= item.AutoPosition.CommissionEvents.Where(x => x.Asset == item.Symbol.QuoteAsset).Sum(x => x.Commission);
            }

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

        private bool TryEnter(SymbolData item, IList<PositionLot> lots, out IAlgoCommand command)
        {
            command = Noop();

            // skip if the symbol is on buy cooldown
            if (lots.Count > 0 && lots[^1].Time.AddDays(1) >= Context.TickTime)
            {
                return false;
            }

            var stopPrice = decimal.MaxValue;

            // get the atr for reference
            var atr = item.Klines.AverageTrueRanges().Last();

            // predict the next kdj cross from oversold
            var oversold = item.Klines.SkipLast(1).Kdj().Reverse().TakeWhile(x => x.Side == KdjSide.Down).Any(x => x.J <= 20);
            var safe1 = item.Klines.SkipLast(1).ParabolicStopAndReverse().Last().Direction == PsarDirection.Long;
            var safe2 = item.Klines.SkipLast(1).Macd(x => x.ClosePrice).Last().IsUptrend;
            if ((oversold || safe1 || safe2) && item.Klines.SkipLast(1).TryGetKdjForUpcross(item.Klines[^1], out var cross))
            {
                var target = item.Symbol.LowerPriceToTickSize(cross.Price);

                // guard - never average down
                if (lots.Count > 0 && lots[^1].AvgPrice > target)
                {
                    return false;
                }

                /*
                // guard - cross must be within the atr to avoid chasing peaks
                var diff = Math.Abs(target - item.Klines[^2].ClosePrice);
                if (diff > atr)
                {
                    return false;
                }
                */

                if (item.Ticker.ClosePrice < target)
                {
                    stopPrice = Math.Min(stopPrice, target);
                }
            }

            if (stopPrice == decimal.MaxValue)
            {
                return false;
            }

            // calculate the buy price window from the stop
            var buyPrice = stopPrice;

            // define the quantity to buy
            var quantity = CalculateBuyQuantity(item, buyPrice);

            command = Sequence(
                EnsureSpotBalance(item.Symbol.QuoteAsset, item.Symbol.RaisePriceToTickSize(quantity * buyPrice), true, true),
                EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.StopLossLimit, TimeInForce.GoodTillCanceled, quantity, null, buyPrice, buyPrice, EntryBuyTag));
            return true;
        }

        private bool TryExit(SymbolData item, IList<PositionLot> lots, PositionStats stats, out IAlgoCommand command)
        {
            command = Noop();

            // there must be something to sell
            if (stats.TotalQuantity == 0)
            {
                return false;
            }

            var stopPrice = 0M;

            // predict the next kdj cross from overbought
            var overbought = item.Klines.SkipLast(1).Kdj().Reverse().TakeWhile(x => x.Side == KdjSide.Up).Any(x => x.J >= 80);
            if (overbought && item.Klines.SkipLast(1).TryGetKdjForDowncross(item.Klines[^1], out var cross))
            {
                var target = item.Symbol.RaisePriceToTickSize(cross.Price);

                if (item.Ticker.ClosePrice > target)
                {
                    stopPrice = Math.Max(stopPrice, target);
                }
            }

            // predict the next kdj cross from overbought
            if (overbought && item.Klines.SkipLast(1).TryGetKdjForDivergenceDowncross(item.Klines[^1], out cross, j: 100))
            {
                var target = item.Symbol.RaisePriceToTickSize(cross.Price);

                if (item.Ticker.ClosePrice > target)
                {
                    stopPrice = Math.Max(stopPrice, target);
                }
            }

            if (stopPrice == 0M)
            {
                return false;
            }

            // skip if there is already an order at a higher price
            /*
            if (item.Orders.Open.Any(x => x.Side == OrderSide.Sell && x.StopPrice >= stopPrice))
            {
                return false;
            }
            */

            // calculate sell window
            var sellPrice = item.Symbol.LowerPriceToTickSize(stopPrice * (1 - 0.01M));

            var quantity = stats.TotalQuantity;

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

        #endregion Logging
    }
}