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
                var lots = item.AutoPosition.Positions.Reverse().EnumerateLots(item.Symbol.Filters.LotSize.StepSize);
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

        private bool TryEnter(SymbolData item, IEnumerable<PositionLot> lots, out IAlgoCommand command)
        {
            command = Noop();

            // skip if on buy cooldown
            var lastLot = lots.FirstOrDefault();
            var cooldown = lastLot.Time.Add(Context.KlineInterval.ToTimeSpan());
            if (cooldown >= Context.TickTime)
            {
                LogEntrySkippedSymbolOnCooldown(TypeName, Context.Name, item.Symbol.Name, cooldown);
                return false;
            }

            // symbol must be under the rmi low momentum threshold
            var rmi = item.Klines.LastRmi(x => x.ClosePrice, _options.Rmi.MomentumPeriods, _options.Rmi.RmaPeriods);
            if (rmi >= _options.Rmi.Low)
            {
                LogEntrySkippedSymbolWithHighCurrentRmi(TypeName, Context.Name, item.Symbol.Name, rmi);
                return false;
            }

            // calculate the price required for crossing the low rmi threshold upwards
            if (!item.Klines.TryGetPriceForRmi(x => x.ClosePrice, _options.Rmi.Low, out var entryPrice, _options.Rmi.MomentumPeriods, _options.Rmi.RmaPeriods, _options.Rmi.Precision))
            {
                LogEntrySkippedSymbolWithUnknownRmiPrice(TypeName, Context.Name, item.Symbol.Name, _options.Rmi.Low);
                return false;
            }
            entryPrice = item.Symbol.LowerPriceToTickSize(entryPrice);

            // calculate the notional to use for buying
            var notional = _options.Notional.AdjustTotalUpToMinNotional(item.Symbol);

            // top up with the fee
            notional *= (1 + (2 * _options.FeeRate));

            // top up with past realized profits
            if (_options.UseProfits)
            {
                notional += item.AutoPosition.ProfitEvents.Sum(x => x.Profit);
                notional -= item.AutoPosition.CommissionEvents.Where(x => x.Asset == item.Symbol.QuoteAsset).Sum(x => x.Commission);
            }

            // calculate the exact quantity for the stop loss limit as it does not accept notional
            var quantity = notional / entryPrice;
            quantity = quantity.AdjustQuantityUpToMinLotSizeQuantity(item.Symbol);
            quantity = quantity.AdjustQuantityUpToLotStepSize(item.Symbol);

            command = Sequence(
                EnsureSpotBalance(item.Symbol.QuoteAsset, quantity * entryPrice, true, true),
                EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.StopLossLimit, TimeInForce.GoodTillCanceled, quantity, null, entryPrice, entryPrice, EntryBuyTag));
            return true;
        }

        private bool TryExit(SymbolData item, IEnumerable<PositionLot> lots, PositionStats stats, out IAlgoCommand command)
        {
            command = Noop();

            // quantity must be sellable
            if (stats.TotalQuantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                LogExitSkippedSymbolWithQuantityUnderMinLotSize(TypeName, Context.Name, item.Symbol.Name, stats.TotalQuantity, item.Symbol.Filters.LotSize.MinQuantity, item.Symbol.BaseAsset);
                return false;
            }

            // symbol must be above the rmi high momentum threshold
            var rmi = item.Klines.LastRmi(x => x.ClosePrice, _options.Rmi.MomentumPeriods, _options.Rmi.RmaPeriods);
            if (rmi < _options.Rmi.High)
            {
                LogExitSkippedSymbolWithRmiUnderHighThreshold(TypeName, Context.Name, item.Symbol.Name, rmi, _options.Rmi.High);
                return false;
            }

            // calculate the price required for crossing the high rmi threshold downwards
            if (!item.Klines.TryGetPriceForRmi(x => x.ClosePrice, _options.Rmi.High, out var exitPrice, _options.Rmi.MomentumPeriods, _options.Rmi.RmaPeriods, _options.Rmi.Precision))
            {
                LogExitSkippedSymbolWithUnknownRmiPrice(TypeName, Context.Name, item.Symbol.Name, _options.Rmi.High);
                return false;
            }
            exitPrice = item.Symbol.LowerPriceToTickSize(exitPrice);

            // gather all the lots that fit under the exit price from the end
            var quantity = 0M;
            var notional = 0M;
            var electedQuantity = 0M;
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

                // the average cost price must fit under the sell price to break even
                var avgPrice = notional / quantity;
                avgPrice *= 1 + (2 * _options.FeeRate);
                avgPrice = item.Symbol.RaisePriceToTickSize(avgPrice);
                if (avgPrice <= exitPrice)
                {
                    // keep the candidate quantity and continue looking for more
                    electedQuantity = quantity;
                }
            }

            if (electedQuantity <= 0)
            {
                LogExitSkippedSymbolWithoutLotsUnderExitPrice(TypeName, Context.Name, item.Symbol.Name, exitPrice, item.Symbol.QuoteAsset);
                return false;
            }

            // place the exit order now
            command = Sequence(
                EnsureSpotBalance(item.Symbol.QuoteAsset, notional, true, true),
                EnsureSingleOrder(item.Symbol, OrderSide.Sell, OrderType.StopLossLimit, TimeInForce.GoodTillCanceled, stats.TotalQuantity, null, exitPrice, exitPrice, ExitSellTag));
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

        #endregion Logging
    }
}