using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class SignificantOrderResolver : ISignificantOrderResolver
    {
        private readonly ILogger _logger;
        private readonly IOrderProvider _orders;
        private readonly ITradeProvider _trades;

        public SignificantOrderResolver(ILogger<SignificantOrderResolver> logger, IOrderProvider orders, ITradeProvider trades)
        {
            _logger = logger;
            _orders = orders;
            _trades = trades;
        }

        private static string Name => nameof(SignificantOrderResolver);

        private sealed record Map(OrderQueryResult Order, AccountTrade Trade)
        {
            public decimal RemainingExecutedQuantity { get; set; }
        }

        public async Task<SignificantResult> ResolveAsync(Symbol symbol, CancellationToken cancellationToken = default)
        {
            var orders = await _orders
                .GetOrdersByFilterAsync(symbol.Name, null, false, true, cancellationToken)
                .ConfigureAwait(false);

            var trades = await _trades
                .GetTradesAsync(symbol.Name, cancellationToken)
                .ConfigureAwait(false);

            return ResolveCore(symbol, orders, trades);
        }

        private SignificantResult ResolveCore(Symbol symbol, IReadOnlyList<OrderQueryResult> orders, IEnumerable<AccountTrade> trades)
        {
            var watch = Stopwatch.StartNew();

            var (mapping, commissions) = Combine(symbol, orders, trades);

            // now prune the significant trades to account interim sales
            using var subjects = ArrayPool<Map>.Shared.RentSegmentWith(mapping);

            // keep track of profit
            var profits = ImmutableList.CreateBuilder<ProfitEvent>();

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

                            // create a profit event
                            profits.Add(new ProfitEvent(
                                symbol,
                                sell.Trade.Time,
                                buy.Order.OrderId,
                                buy.Trade.Id,
                                sell.Order.OrderId,
                                sell.Trade.Id,
                                take,
                                buy.Trade.Price,
                                sell.Trade.Price));

                            // if the sale is filled then we can break early
                            if (sell.RemainingExecutedQuantity == 0) break;
                        }
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
            var significant = subjects.Segment
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
                    x.Key.OriginalQuoteOrderQuantity))
                .ToImmutableSortedSet(OrderQueryResult.KeyComparer);

            _logger.LogInformation(
                "{Name} {Symbol} identified {Count} significant orders in {ElapsedMs}ms",
                nameof(SignificantOrderResolver), symbol.Name, significant.Count, watch.ElapsedMilliseconds);

            return new SignificantResult(symbol, significant, profits.ToImmutable(), commissions);
        }

        private (SortedSet<Map> Mapping, ImmutableList<CommissionEvent> Commissions) Combine(Symbol symbol, IEnumerable<OrderQueryResult> orders, IEnumerable<AccountTrade> trades)
        {
            var lookup = trades.ToLookup(x => x.OrderId);

            var mapping = new SortedSet<Map>(MapComparer.Instance);
            var commissions = ImmutableList.CreateBuilder<CommissionEvent>();

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

                    // log the commission event regardless
                    commissions.Add(new CommissionEvent(
                        symbol,
                        trade.Time,
                        trade.OrderId,
                        trade.Id,
                        trade.CommissionAsset,
                        trade.Commission));

                    mapping.Add(map);

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

            return (mapping, commissions.ToImmutable());
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
    }
}