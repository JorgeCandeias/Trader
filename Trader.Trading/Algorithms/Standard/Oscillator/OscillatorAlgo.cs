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

                commands.Add(
                    TrySell(item, stats) ??
                    TryEntryBuy(item, stats) ??
                    Noop());
            }

            return Sequence(commands);
        }

        private IAlgoCommand? TrySell(SymbolData item, PositionStats stats)
        {
            // skip unsellable symbol
            var quantity = stats.TotalQuantity;
            if (quantity < item.Symbol.Filters.LotSize.MinQuantity)
            {
                return null;
            }

            // calculate the ideal sell price
            var sellPrice = item.Klines.PriceForRsi(x => x.ClosePrice, _options.Rsi.Periods, _options.Rsi.Overbought, _options.Rsi.Precision);

            // calculate the elastic stop loss rate based on the price amplitude
            var stopLossRate = MathD.LerpBetween(stats.AvgPrice, sellPrice, item.Ticker.ClosePrice, _options.StopLossRate.Max, _options.StopLossRate.Min);

            // calculate the reference price for the stop loss price
            var highPrice = Math.Max(stats.AvgPrice, item.Ticker.ClosePrice);

            // calculate the stop loss price
            var stopLossPrice = highPrice * (1 - stopLossRate);
            stopLossPrice = stopLossPrice.AdjustPriceDownToTickSize(item.Symbol);
            var under = stopLossPrice - item.Symbol.Filters.Price.TickSize;

            // skip unsellable symbol
            var sellable = quantity * under;
            if (sellable < item.Symbol.Filters.MinNotional.MinNotional)
            {
                return null;
            }

            // skip if there is already a limit sell order active (the stop loss triggered)
            if (item.Orders.Open.Any(x => x.Type == OrderType.Limit && x.Side == OrderSide.Sell))
            {
                return null;
            }

            // skip if there is already a stop loss at a higher value than what we calculate (the price went up then down since purchase)
            if (item.Orders.Open.Any(x => x.Type == OrderType.StopLossLimit && x.Side == OrderSide.Sell && x.OriginalQuantity == quantity && x.StopPrice >= stopLossPrice))
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

            // calculate the notional to use for buying
            var notional = _options.Notional.AdjustTotalUpToMinNotional(item.Symbol);

            // top up with the fee
            notional *= (1 + _options.FeeRate);

            // top up with past realized profits
            if (_options.UseProfits)
            {
                notional += item.AutoPosition.ProfitEvents.Sum(x => x.Profit);
                notional -= item.AutoPosition.CommissionEvents.Where(x => x.Asset == item.Symbol.QuoteAsset).Sum(x => x.Commission);
            }

            // calculate the quantity quantity for the notional
            quantity = notional / lowPrice;
            quantity = quantity.AdjustQuantityUpToMinLotSizeQuantity(item.Symbol);
            quantity = quantity.AdjustQuantityUpToLotStepSize(item.Symbol);

            return EnsureSingleOrder(item.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, lowPrice, null, true, true);
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} skipped invalidated symbol {Symbol}")]
        private partial void LogSkippedInvalidatedSymbol(string type, string name, string symbol);

        #endregion Logging
    }
}