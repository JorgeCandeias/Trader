using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    internal class SignificantOrderResolver : ISignificantOrderResolver
    {
        private readonly ILogger _logger;
        private readonly ITraderRepository _repository;

        public SignificantOrderResolver(ILogger<SignificantOrderResolver> logger, ITraderRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<SortedOrderSet> ResolveAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var orders = await _repository.GetOrdersAsync(symbol, cancellationToken);
            var trades = await _repository.GetTradesAsync(symbol, cancellationToken);

            // todo: keep track of the last significant order start so we avoid slowing down when the orders grow and grow
            // todo: remove the first step and go straight to lifo processing over the entire order set
            // todo: persist all this stuff into sqlite so each tick can operate over the last data only

            // index the trades for quick lookup
            var tradesByOrderId = trades.ToLookup(x => x.OrderId);

            // match significant orders to trades so we can sort significant orders by execution date
            var map = new SortedOrderTradeMapSet();
            foreach (var order in orders)
            {
                if (order.ExecutedQuantity > 0m)
                {
                    map.Add(new OrderTradeMap(order, tradesByOrderId[order.OrderId]));
                }
            }

            // now prune the significant trades to account interim sales
            using var subjects = ArrayPool<OrderTradeMap>.Shared.RentSegmentFrom(map);

            for (var i = 0; i < subjects.Segment.Count; ++i)
            {
                // loop through sales forward
                var sell = subjects.Segment[i];
                if (sell.Order.Side == OrderSide.Sell)
                {
                    // loop through buys in lifo order to find the matching buy
                    for (var j = i - 1; j >= 0; --j)
                    {
                        var buy = subjects.Segment[j];
                        if (buy.Order.Side == OrderSide.Buy)
                        {
                            // remove as much as possible from the buy to satisfy the sell
                            var take = Math.Min(buy.RemainingExecutedQuantity, sell.RemainingExecutedQuantity);
                            buy.RemainingExecutedQuantity -= take;
                            sell.RemainingExecutedQuantity -= take;

                            // if the sale is filled then we can break early
                            if (sell.RemainingExecutedQuantity == 0) break;
                        }
                    }

                    // if the sell was not filled then we're missing some data
                    if (sell.RemainingExecutedQuantity != 0)
                    {
                        // something went very wrong if we got here
                        throw new AlgorithmException($"{GetType().Name} could not fill {sell.Order.Symbol} {sell.Order.Side} order {sell.Order.OrderId} with quantity {sell.Order.ExecutedQuantity} at price {sell.Order.Price}");
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
                        subject.Order.Price,
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
                "{Name} {Symbol} identified {Count} significant orders",
                nameof(SignificantOrderResolver), symbol, significant.Count);

            return significant;
        }
    }
}