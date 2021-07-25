using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;
using Trader.Models.Collections;

namespace Trader.Trading.Algorithms
{
    internal class SignificantOrderResolver : ISignificantOrderResolver
    {
        private readonly ILogger _logger;
        private readonly ITradingRepository _repository;
        private readonly ISystemClock _clock;

        public SignificantOrderResolver(ILogger<SignificantOrderResolver> logger, ITradingRepository repository, ISystemClock clock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Name => nameof(SignificantOrderResolver);

        private record Map(OrderQueryResult Order, AccountTrade Trade)
        {
            public decimal RemainingExecutedQuantity { get; set; }
        }

        private class MapComparer : IComparer<Map>
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

        public async Task<SignificantResult> ResolveAsync(string symbol, string quote, CancellationToken cancellationToken = default)
        {
            // todo: request the Symbol object instead of the separate parameters

            // todo: push this mapping effort to the repository so less data needs to move about
            var watch = Stopwatch.StartNew();
            var orders = await _repository
                .GetSignificantCompletedOrdersAsync(symbol, cancellationToken)
                .ConfigureAwait(false);

            var trades = await _repository
                .GetTradesAsync(symbol, cancellationToken)
                .ConfigureAwait(false);

            var lookup = trades.ToLookup(x => x.OrderId);

            var details = new SortedSet<Map>(MapComparer.Instance);
            foreach (var order in orders)
            {
                var quantity = 0m;

                foreach (var trade in lookup[order.OrderId])
                {
                    details.Add(new Map(order, trade)
                    {
                        RemainingExecutedQuantity = trade.Quantity
                    });

                    quantity += trade.Quantity;
                }

                if (quantity != order.ExecutedQuantity)
                {
                    // we have missing trades if this happened
                    _logger.LogError(
                        "{Name} {Symbol} could not match {OrderSide} {OrderType} {OrderId} at {Time} for {ExecutedQuantity} units with total trade quantity of {TradeQuantity}",
                        Name, symbol, order.Side, order.Type, order.OrderId, order.Time, order.ExecutedQuantity, quantity);
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
            var d1 = 0m;
            var d7 = 0m;
            var d30 = 0m;

            // hold the current time so profit assignments are consistent
            var now = _clock.UtcNow;
            var window1d = now.AddDays(-1);
            var window7d = now.AddDays(-7);
            var window30d = now.AddDays(-30);
            var today = now.Date;

            // first map formal limit sells to formal buys
            // the sells may not fill completely due to past algo bugs and missing trades
            for (var i = 0; i < subjects.Segment.Count; ++i)
            {
                // loop through sales forward
                var sell = subjects.Segment[i];
                if (sell.Order.Side == OrderSide.Sell && sell.Order.Type == OrderType.Limit && sell.RemainingExecutedQuantity > 0 && long.TryParse(sell.Order.ClientOrderId, out var matchOpenOrderId) && matchOpenOrderId > 0)
                {
                    // look through buys backwards from the sale
                    for (var j = i - 1; j >= 0; --j)
                    {
                        var buy = subjects.Segment[j];
                        if (buy.Order.Side == OrderSide.Buy && buy.RemainingExecutedQuantity > 0 && buy.Order.OrderId == matchOpenOrderId)
                        {
                            // remove as much as possible from the buy to satisfy the sell
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

                            // assign to the window counters
                            if (sell.Trade.Time >= window1d) d1 += profit;
                            if (sell.Trade.Time >= window7d) d7 += profit;
                            if (sell.Trade.Time >= window30d) d30 += profit;

                            // if the sale is filled then we can break early
                            if (sell.RemainingExecutedQuantity == 0) break;
                        }
                    }
                }
            }

            // now match limit sell leftovers using lifo
            for (var i = 0; i < subjects.Segment.Count; ++i)
            {
                // loop through sales forward
                var sell = subjects.Segment[i];
                if (sell.Order.Side == OrderSide.Sell && sell.Order.Type == OrderType.Limit && sell.RemainingExecutedQuantity > 0m)
                {
                    // loop through buys in lifo order to find matching buys
                    for (var j = i - 1; j >= 0; --j)
                    {
                        var buy = subjects.Segment[j];
                        if (buy.Order.Side == OrderSide.Buy && buy.RemainingExecutedQuantity > 0m)
                        {
                            // remove as much as possible from the buy to satisfy the sell
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

                            // assign to the window counters
                            if (sell.Trade.Time >= window1d) d1 += profit;
                            if (sell.Trade.Time >= window7d) d7 += profit;
                            if (sell.Trade.Time >= window30d) d30 += profit;

                            // if the sale is filled then we can break early
                            if (sell.RemainingExecutedQuantity == 0) break;
                        }
                    }

                    // if the sell was still not filled then we're missing some data
                    if (sell.RemainingExecutedQuantity != 0)
                    {
                        // something went very wrong if we got here
                        _logger.LogWarning(
                            "{Name} {Symbol} could not fill {Symbol} {Side} order {OrderId} with quantity {ExecutedQuantity} at price {Price} because there are {RemainingExecutedQuantity} units missing",
                            nameof(SignificantOrderResolver), symbol, sell.Order.Symbol, sell.Order.Side, sell.Order.OrderId, sell.Order.ExecutedQuantity, sell.Order.Price, sell.RemainingExecutedQuantity);
                    }
                }
            }

            // now match market sell leftovers using fifo
            // we use market sell orders to get rid of old leftovers manually when the asset is peaking in price
            for (var i = 0; i < subjects.Segment.Count; ++i)
            {
                // loop through sales forward
                var sell = subjects.Segment[i];
                if (sell.Order.Side == OrderSide.Sell && sell.Order.Type == OrderType.Market && sell.RemainingExecutedQuantity > 0m)
                {
                    // loop through buys in lifo order to find matching buys
                    for (var j = 0; j < i; j++)
                    {
                        var buy = subjects.Segment[j];
                        if (buy.Order.Side == OrderSide.Buy && buy.RemainingExecutedQuantity > 0m)
                        {
                            // remove as much as possible from the buy to satisfy the sell
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

                            // assign to the window counters
                            if (sell.Trade.Time >= window1d) d1 += profit;
                            if (sell.Trade.Time >= window7d) d7 += profit;
                            if (sell.Trade.Time >= window30d) d30 += profit;

                            // if the sale is filled then we can break early
                            if (sell.RemainingExecutedQuantity == 0) break;
                        }
                    }

                    // if the sell was still not filled then we're missing some data
                    if (sell.RemainingExecutedQuantity != 0)
                    {
                        // something went very wrong if we got here
                        _logger.LogWarning(
                            "{Name} {Symbol} could not fill {Symbol} {Side} order {OrderId} with quantity {ExecutedQuantity} at price {Price} because there are {RemainingExecutedQuantity} units missing",
                            nameof(SignificantOrderResolver), symbol, sell.Order.Symbol, sell.Order.Side, sell.Order.OrderId, sell.Order.ExecutedQuantity, sell.Order.Price, sell.RemainingExecutedQuantity);
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
                nameof(SignificantOrderResolver), symbol, significant.Count, watch.ElapsedMilliseconds);

            var summary = new Profit(
                quote,
                todayProfit,
                yesterdayProfit,
                thisWeekProfit,
                prevWeekProfit,
                thisMonthProfit,
                thisYearProfit,
                d1,
                d7,
                d30);

            return new SignificantResult(significant.ToImmutable(), summary);
        }
    }
}