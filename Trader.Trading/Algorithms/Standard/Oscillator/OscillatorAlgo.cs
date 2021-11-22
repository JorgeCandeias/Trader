using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Outcompute.Trader.Trading.Commands;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Oscillator
{
    internal partial class OscillatorAlgo : Algo
    {
        private readonly OscillatorAlgoOptions _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;

        public OscillatorAlgo(IOptionsSnapshot<OscillatorAlgoOptions> options, ILogger<OscillatorAlgo> logger, ISystemClock clock)
        {
            _options = options.Get(Context.Name);
            _logger = logger;
            _clock = clock;
        }

        private const string TypeName = nameof(OscillatorAlgo);

        protected override IAlgoCommand OnExecute()
        {
            var commands = new List<IAlgoCommand>(Context.Data.Count);

            foreach (var item in Context.Data)
            {
                // skip invalidated symbol
                if (!item.IsValid)
                {
                    LogSkippedInvalidatedSymbol(TypeName, Context.Name, item.Symbol.Name);
                    continue;
                }

                // get pv stats over sellable positions from the last
                var stats = item.AutoPosition.Positions.Reverse().EnumerateLots(item.Symbol.Filters.LotSize.StepSize).GetStats(item.Ticker.ClosePrice);

                // calculate the current rsi
                var rsi = item.Klines.LastRsi(x => x.ClosePrice, _options.Rsi.Periods);

                commands.Add(
                    TryTakeProfit(item, stats, rsi) ??
                    TryStopLoss(item, stats) ??
                    TryEntryBuy(item, stats) ??
                    Noop());
            }

            return Sequence(commands);
        }

        private IAlgoCommand? TryTakeProfit(SymbolData item, PositionStats stats, decimal rsi)
        {
            // skip unsellable symbol
            var quantity = stats.TotalQuantity;
            if (quantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                return null;
            }

            // skip if the ticker is downside
            if (item.Ticker.ClosePrice <= stats.AvgPrice)
            {
                return null;
            }

            // calculate take profit price
            var takeProfitPrice = stats.AvgPrice * (1 + _options.TakeProfitRate);
            takeProfitPrice = takeProfitPrice.AdjustPriceUpToTickSize(item.Symbol);
            var above = takeProfitPrice + item.Symbol.Filters.Price.TickSize;

            // skip unsellable symbol
            var sellable = quantity * above;
            if (sellable < item.Symbol.Filters.MinNotional.MinNotional)
            {
                return null;
            }

            // if the current rsi is already overbought and we can break even then raise a market sell order
            if (rsi >= _options.Rsi.Overbought && stats.RelativePnL > 0)
            {
                // if there is an open market order then skip to let the system update itself
                if (item.Orders.Open.Any(x => x.Type == OrderType.Market))
                {
                    return null;
                }

                // issue the market order
                return Sequence(
                    CancelOpenOrders(item.Symbol, OrderSide.Buy),
                    MarketSell(item.Symbol, quantity, true, true));
            }

            // skip if not yet halfway there (prevents order twitching when the ticker is near the buy price)
            var halfway = (stats.AvgPrice + takeProfitPrice) / 2;
            if (item.Ticker.ClosePrice < halfway)
            {
                return null;
            }

            // skip if there is already a limit order active for it (the take profit triggered)
            if (item.Orders.Open.Any(x => x.Type == OrderType.Limit && x.Side == OrderSide.Sell && x.OriginalQuantity == quantity && x.Price == above))
            {
                return null;
            }

            // set the order for it
            return Sequence(
                CancelOpenOrders(item.Symbol, OrderSide.Buy),
                EnsureSingleOrder(item.Symbol, OrderSide.Sell, OrderType.TakeProfitLimit, TimeInForce.GoodTillCanceled, quantity, above, takeProfitPrice, true, true));
        }

        private IAlgoCommand? TryStopLoss(SymbolData item, PositionStats stats)
        {
            // skip unsellable symbol
            var quantity = stats.TotalQuantity;
            if (quantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                return null;
            }

            // skip if the ticker is upside
            if (item.Ticker.ClosePrice >= stats.AvgPrice)
            {
                return null;
            }

            // calculate stop loss price
            var stopLossPrice = stats.AvgPrice * (1 - _options.StopLossRate);
            stopLossPrice = stopLossPrice.AdjustPriceDownToTickSize(item.Symbol);
            var under = stopLossPrice - item.Symbol.Filters.Price.TickSize;

            // skip unsellable symbol
            var sellable = quantity * under;
            if (sellable < item.Symbol.Filters.MinNotional.MinNotional)
            {
                return null;
            }

            // skip if there is already a limit order active for it (the stop loss triggered)
            if (item.Orders.Open.Any(x => x.Type == OrderType.Limit && x.Side == OrderSide.Sell && x.OriginalQuantity == quantity && x.Price == under))
            {
                return null;
            }

            // skip if not yet halfway there (prevents order twitching when the ticker is near the buy price)
            var halfway = (stats.AvgPrice + stopLossPrice) / 2;
            if (item.Ticker.ClosePrice > halfway)
            {
                return null;
            }

            // set the order for it
            return Sequence(
                CancelOpenOrders(item.Symbol, OrderSide.Buy),
                EnsureSingleOrder(item.Symbol, OrderSide.Sell, OrderType.StopLossLimit, TimeInForce.GoodTillCanceled, quantity, under, stopLossPrice, false, false));
        }

        private IAlgoCommand? TryEntryBuy(SymbolData item, PositionStats stats)
        {
            // skip sellable symbol
            var quantity = stats.TotalQuantity;
            var sellable = quantity * item.Ticker.ClosePrice;
            if (quantity >= item.Symbol.Filters.LotSize.MinQuantity && sellable >= item.Symbol.Filters.MinNotional.MinNotional)
            {
                return null;
            }

            // calculate the target price for the entry rsi
            var lowPrice = item.Klines.PriceForRsi(x => x.ClosePrice, _options.Rsi.Periods, _options.Rsi.Oversold, _options.Rsi.Precision);
            lowPrice = lowPrice.AdjustPriceDownToTickSize(item.Symbol);

            // skip if price below allowed price
            if (lowPrice < item.Symbol.Filters.Price.MinPrice)
            {
                return null;
            }

            // calculate the target quantity for the target price
            var notional = _options.Notional.AdjustTotalUpToMinNotional(item.Symbol);
            quantity = notional / lowPrice;
            if (_options.UseProfits)
            {
                quantity += item.AutoPosition.ProfitEvents.Sum(x => x.Profit);
                quantity -= item.AutoPosition.CommissionEvents.Where(x => x.Asset == item.Symbol.BaseAsset).Sum(x => x.Commission);
            }
            quantity = quantity.AdjustQuantityUpToMinLotSizeQuantity(item.Symbol);
            quantity = quantity.AdjustQuantityUpToLotStepSize(item.Symbol);
            quantity *= (1 + _options.FeeRate);
            quantity = quantity.AdjustQuantityUpToLotStepSize(item.Symbol);

            return EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, lowPrice, null, false, false);
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} skipped invalidated symbol {Symbol}")]
        private partial void LogSkippedInvalidatedSymbol(string type, string name, string symbol);

        #endregion Logging
    }
}