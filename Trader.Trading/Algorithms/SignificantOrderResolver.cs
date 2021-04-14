using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    internal class SignificantOrderResolver : ISignificantOrderResolver
    {
        private readonly ILogger _logger;
        private readonly ITraderRepository _repository;
        private readonly ISystemClock _clock;

        public SignificantOrderResolver(ILogger<SignificantOrderResolver> logger, ITraderRepository repository, ISystemClock clock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<SignificantResult> ResolveAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var watch = Stopwatch.StartNew();
            var orders = await _repository.GetOrdersAsync(symbol, cancellationToken);

            // match significant orders to trades so we can sort significant orders by execution date
            var map = new SortedOrderTradeMapSet();
            foreach (var order in orders)
            {
                if (order.ExecutedQuantity > 0m)
                {
                    var trades = await _repository.GetTradesAsync(symbol, order.OrderId, cancellationToken);
                    map.Add(new OrderTradeMap(order, trades));
                }
            }

            // now prune the significant trades to account interim sales
            using var subjects = ArrayPool<OrderTradeMap>.Shared.RentSegmentFrom(map);

            // keep track of profit
            var todayProfit = 0m;
            var yesterdayProfit = 0m;
            var thisWeekProfit = 0m;
            var prevWeekProfit = 0m;
            var thisMonthProfit = 0m;
            var thisYearProfit = 0m;

            // hold the current time so profit assignments are consistent
            var now = _clock.UtcNow;
            var today = now.Date;

            // first map formal sells to formal buys
            for (var i = 0; i < subjects.Segment.Count; ++i)
            {
                // loop through sales forward
                var sell = subjects.Segment[i];
                if (sell.Order.Side == OrderSide.Sell && sell.RemainingExecutedQuantity > 0 && long.TryParse(sell.Order.ClientOrderId, out var matchOpenOrderId) && matchOpenOrderId > 0)
                {
                    for (var j = i - 1; j >= 0; --j)
                    {
                        var buy = subjects.Segment[j];
                        if (buy.Order.Side == OrderSide.Buy && buy.RemainingExecutedQuantity > 0 && buy.Order.OriginalQuantity == sell.Order.OriginalQuantity && buy.Order.OrderId == matchOpenOrderId)
                        {
                            // remove as much as possible from the buy to satisfy the sell
                            var take = Math.Min(buy.RemainingExecutedQuantity, sell.RemainingExecutedQuantity);
                            buy.RemainingExecutedQuantity -= take;
                            sell.RemainingExecutedQuantity -= take;

                            // calculate profit for this
                            var profit = take * (sell.AvgTradePrice - buy.AvgTradePrice);

                            // assign to the appropriate counters
                            if (sell.MaxEventTime.Date == today) todayProfit += profit;
                            if (sell.MaxEventTime.Date == today.AddDays(-1)) yesterdayProfit += profit;
                            if (sell.MaxEventTime.Date >= today.Previous(DayOfWeek.Sunday)) thisWeekProfit += profit;
                            if (sell.MaxEventTime.Date >= today.Previous(DayOfWeek.Sunday, 2) && sell.MaxEventTime.Date < today.Previous(DayOfWeek.Sunday)) prevWeekProfit += profit;
                            if (sell.MaxEventTime.Date >= today.AddDays(-today.Day + 1)) thisMonthProfit += profit;
                            if (sell.MaxEventTime.Date >= new DateTime(today.Year, 1, 1)) thisYearProfit += profit;

                            // leave the sale as-is regardless of fill
                            break;
                        }
                    }
                }
            }

            // now match leftovers using lifo
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
                            // remove as much as possible from the buy to satisfy the sell
                            var take = Math.Min(buy.RemainingExecutedQuantity, sell.RemainingExecutedQuantity);
                            buy.RemainingExecutedQuantity -= take;
                            sell.RemainingExecutedQuantity -= take;

                            // calculate profit for this
                            var profit = take * (sell.AvgTradePrice - buy.AvgTradePrice);

                            // assign to the appropriate counters
                            if (sell.MaxEventTime.Date == today) todayProfit += profit;
                            if (sell.MaxEventTime.Date == today.AddDays(-1)) yesterdayProfit += profit;
                            if (sell.MaxEventTime.Date >= today.Previous(DayOfWeek.Sunday)) thisWeekProfit += profit;
                            if (sell.MaxEventTime.Date >= today.Previous(DayOfWeek.Sunday, 2) && sell.MaxEventTime.Date < today.Previous(DayOfWeek.Sunday)) prevWeekProfit += profit;
                            if (sell.MaxEventTime.Date >= today.AddDays(-today.Day + 1)) thisMonthProfit += profit;
                            if (sell.MaxEventTime.Date >= new DateTime(today.Year, 1, 1)) thisYearProfit += profit;

                            // if the sale is filled then we can break early
                            if (sell.RemainingExecutedQuantity == 0) break;
                        }
                    }

                    // if the sell was not filled then we're missing some data
                    if (sell.RemainingExecutedQuantity != 0)
                    {
                        // something went very wrong if we got here
                        _logger.LogError(
                            "{Name} {Symbol} could not fill {Symbol} {Side} order {OrderId} with quantity {ExecutedQuantity} at price {Price}",
                            nameof(SignificantOrderResolver), symbol, sell.Order.Symbol, sell.Order.Side, sell.Order.OrderId, sell.Order.ExecutedQuantity, sell.Order.Price);
                    }
                }
            }

            // keep only buys with some quantity left to sell
            var significant = new SortedOrderSet();
            foreach (var subject in subjects.Segment)
            {
                if (subject.Order.Side == OrderSide.Buy && subject.RemainingExecutedQuantity > 0)
                {
                    significant.Add(new OrderQueryResult(
                        subject.Order.Symbol,
                        subject.Order.OrderId,
                        subject.Order.OrderListId,
                        subject.Order.ClientOrderId,
                        subject.AvgTradePrice,
                        subject.Order.OriginalQuantity,
                        subject.RemainingExecutedQuantity,
                        subject.Order.CummulativeQuoteQuantity,
                        subject.Order.Status,
                        subject.Order.TimeInForce,
                        subject.Order.Type,
                        subject.Order.Side,
                        subject.Order.StopPrice,
                        subject.Order.IcebergQuantity,
                        subject.Order.Time,
                        subject.Order.UpdateTime,
                        subject.Order.IsWorking,
                        subject.Order.OriginalQuoteOrderQuantity));
                }
            }

            _logger.LogInformation(
                "{Name} {Symbol} identified {Count} significant orders in {ElapsedMs}ms",
                nameof(SignificantOrderResolver), symbol, significant.Count, watch.ElapsedMilliseconds);

            var summary = new Profit(
                todayProfit,
                yesterdayProfit,
                thisWeekProfit,
                prevWeekProfit,
                thisMonthProfit,
                thisYearProfit);

            var stats = new Statistics(
                todayProfit.SafeDivideBy((decimal)now.TimeOfDay.TotalHours));

            return new SignificantResult(significant, summary, stats);
        }
    }
}