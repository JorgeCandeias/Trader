using Microsoft.Extensions.Logging;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class SignificantOrderResolver : ISignificantOrderResolver
    {
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;
        private readonly IOrderProvider _orders;
        private readonly ITradeProvider _trades;

        public SignificantOrderResolver(ILogger<SignificantOrderResolver> logger, ISystemClock clock, IOrderProvider orders, ITradeProvider trades)
        {
            _logger = logger;
            _clock = clock;
            _orders = orders;
            _trades = trades;
        }

        private static string Name => nameof(SignificantOrderResolver);

        private sealed record Map(OrderQueryResult Order, AccountTrade Trade)
        {
            public decimal RemainingExecutedQuantity { get; set; }
        }

        private sealed class MapComparer : IComparer<Map>
        {
            private MapComparer()
            {
            }

            public int Compare(Map? x, Map? y)
            {
                if (x is null) throw new ArgumentNullException(nameof(x));
                if (y is null) throw new ArgumentNullException(nameof(y));

                return Comparer<long>.Default.Compare(x.Trade.Id, y.Trade.Id);
            }

            public static MapComparer Instance { get; } = new MapComparer();
        }

        public async ValueTask<SignificantResult> ResolveAsync(Symbol symbol, CancellationToken cancellationToken = default)
        {
            var orders = await _orders
                .GetSignificantCompletedOrdersAsync(symbol.Name, cancellationToken)
                .ConfigureAwait(false);

            var trades = await _trades
                .GetTradesAsync(symbol.Name, cancellationToken)
                .ConfigureAwait(false);

            return ResolveCore(symbol, orders, trades);
        }

        private SignificantResult ResolveCore(Symbol symbol, IReadOnlyList<OrderQueryResult> orders, IEnumerable<AccountTrade> trades)
        {
            var watch = Stopwatch.StartNew();

            var lookup = trades.ToLookup(x => x.OrderId);

            var details = new SortedSet<Map>(MapComparer.Instance);

            foreach (var order in orders)
            {
                var quantity = 0m;

                foreach (var trade in lookup[order.OrderId])
                {
                    // map the order to the trade so we have info on both
                    var map = new Map(order, trade)
                    {
                        RemainingExecutedQuantity = trade.Quantity
                    };

                    // remove the spent commission from the buy balance if taken from the same asset
                    if (trade.IsBuyer && trade.CommissionAsset == symbol.BaseAsset)
                    {
                        map.RemainingExecutedQuantity -= trade.Commission;
                    }

                    details.Add(map);

                    quantity += trade.Quantity;
                }

                if (quantity != order.ExecutedQuantity)
                {
                    // we have missing trades if this happened
                    _logger.LogError(
                        "{Name} {Symbol} could not match {OrderSide} {OrderType} {OrderId} at {Time} for {ExecutedQuantity} units with total trade quantity of {TradeQuantity}",
                        Name, symbol.Name, order.Side, order.Type, order.OrderId, order.Time, order.ExecutedQuantity, quantity);
                }
            }

            // now prune the significant trades to account interim sales
            using var subjects = ArrayPool<Map>.Shared.RentSegmentWith(details);

            // keep track of profit
            var todayProfit = 0m;
            var yesterdayProfit = 0m;
            var thisWeekProfit = 0m;
            var prevWeekProfit = 0m;
            var thisMonthProfit = 0m;
            var thisYearProfit = 0m;
            var all = 0m;
            var d1 = 0m;
            var d7 = 0m;
            var d30 = 0m;

            // hold the current time so profit assignments are consistent
            var now = _clock.UtcNow;
            var window1d = now.AddDays(-1);
            var window7d = now.AddDays(-7);
            var window30d = now.AddDays(-30);
            var today = now.Date;

            // now match sale leftovers using lifo
            // the sales may not fill completely using the buys due to selling from savings and buy market orders to help fix bugs
            for (var i = 0; i < subjects.Segment.Count; ++i)
            {
                // loop through sales forward
                var sell = subjects.Segment[i];
                if (sell.Order.Side == OrderSide.Sell && sell.RemainingExecutedQuantity > 0m)
                {
                    // loop through buys in lifo order to find matching buys
                    for (var j = i - 1; j >= 0; --j)
                    {
                        var buy = subjects.Segment[j];
                        if (buy.Order.Side == OrderSide.Buy && buy.RemainingExecutedQuantity > 0m)
                        {
                            // remove as much as possible from the buy to satisfy the sale
                            var take = Math.Min(buy.RemainingExecutedQuantity, sell.RemainingExecutedQuantity);
                            buy.RemainingExecutedQuantity -= take;
                            sell.RemainingExecutedQuantity -= take;

                            // calculate profit for this
                            var profit = take * (sell.Trade.Price - buy.Trade.Price);

                            // assign to the appropriate counters
                            if (sell.Trade.Time.Date == today) todayProfit += profit;
                            if (sell.Trade.Time.Date == today.AddDays(-1)) yesterdayProfit += profit;
                            if (sell.Trade.Time.Date >= today.Previous(DayOfWeek.Sunday)) thisWeekProfit += profit;
                            if (sell.Trade.Time.Date >= today.Previous(DayOfWeek.Sunday, 2) && sell.Trade.Time.Date < today.Previous(DayOfWeek.Sunday)) prevWeekProfit += profit;
                            if (sell.Trade.Time.Date >= today.AddDays(-today.Day + 1)) thisMonthProfit += profit;
                            if (sell.Trade.Time.Date >= new DateTime(today.Year, 1, 1)) thisYearProfit += profit;
                            all += profit;

                            // assign to the window counters
                            if (sell.Trade.Time >= window1d) d1 += profit;
                            if (sell.Trade.Time >= window7d) d7 += profit;
                            if (sell.Trade.Time >= window30d) d30 += profit;

                            // if the sale is filled then we can break early
                            if (sell.RemainingExecutedQuantity == 0) break;
                        }
                    }

                    // if the commission was taken from the sell asset then remove it from profit
                    if (sell.Trade.CommissionAsset == symbol.QuoteAsset)
                    {
                        // calculate loss for this
                        var loss = sell.Trade.Commission;

                        // assign to the appropriate counters
                        if (sell.Trade.Time.Date == today) todayProfit -= loss;
                        if (sell.Trade.Time.Date == today.AddDays(-1)) yesterdayProfit -= loss;
                        if (sell.Trade.Time.Date >= today.Previous(DayOfWeek.Sunday)) thisWeekProfit -= loss;
                        if (sell.Trade.Time.Date >= today.Previous(DayOfWeek.Sunday, 2) && sell.Trade.Time.Date < today.Previous(DayOfWeek.Sunday)) prevWeekProfit -= loss;
                        if (sell.Trade.Time.Date >= today.AddDays(-today.Day + 1)) thisMonthProfit -= loss;
                        if (sell.Trade.Time.Date >= new DateTime(today.Year, 1, 1)) thisYearProfit -= loss;
                        all -= loss;

                        // assign to the window counters
                        if (sell.Trade.Time >= window1d) d1 -= loss;
                        if (sell.Trade.Time >= window7d) d7 -= loss;
                        if (sell.Trade.Time >= window30d) d30 -= loss;
                    }

                    // if the sale was still not filled then force close it
                    // we assume the remaining assets used to fullfil the sale came either savings or market conversions
                    // both of which we cant track here
                    if (sell.RemainingExecutedQuantity != 0)
                    {
                        // clear the sale
                        _logger.LogWarning(
                            "{Name} {Symbol} could not fill {Type} {Side} order {OrderId} as there is {Missing} {Asset} missing",
                            nameof(SignificantOrderResolver), symbol.Name, sell.Order.Type, sell.Order.Side, sell.Order.OrderId, sell.RemainingExecutedQuantity, symbol.BaseAsset);

                        sell.RemainingExecutedQuantity = 0m;
                    }
                }
            }

            // keep only buy orders with some quantity left to sell
            var significant = ImmutableSortedOrderSet.CreateBuilder();

            foreach (var survivor in subjects.Segment
                .Where(x => x.Order.Side == OrderSide.Buy && x.RemainingExecutedQuantity > 0m)
                .GroupBy(x => x.Order)
                .Select(x => new OrderQueryResult(
                    x.Key.Symbol,
                    x.Key.OrderId,
                    x.Key.OrderListId,
                    x.Key.ClientOrderId,

                    // market orders will have the price set to zero so we must derive the average from the executed trades
                    x.Key.Price is 0 ? x.Sum(y => y.Trade.Price * y.Trade.Quantity) / x.Sum(y => y.Trade.Quantity) : x.Key.Price,

                    x.Key.OriginalQuantity,
                    x.Sum(y => y.RemainingExecutedQuantity),
                    x.Key.CummulativeQuoteQuantity,
                    x.Key.Status,
                    x.Key.TimeInForce,
                    x.Key.Type,
                    x.Key.Side,
                    x.Key.StopPrice,
                    x.Key.IcebergQuantity,
                    x.Key.Time,
                    x.Key.UpdateTime,
                    x.Key.IsWorking,
                    x.Key.OriginalQuoteOrderQuantity)))
            {
                significant.Add(survivor);
            }

            _logger.LogInformation(
                "{Name} {Symbol} identified {Count} significant orders in {ElapsedMs}ms",
                nameof(SignificantOrderResolver), symbol.Name, significant.Count, watch.ElapsedMilliseconds);

            var summary = new Profit(
                symbol.Name,
                symbol.BaseAsset,
                symbol.QuoteAsset,
                todayProfit,
                yesterdayProfit,
                thisWeekProfit,
                prevWeekProfit,
                thisMonthProfit,
                thisYearProfit,
                all,
                d1,
                d7,
                d30);

            return new SignificantResult(significant.ToImmutable(), summary);
        }
    }
}