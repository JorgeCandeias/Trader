using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Data.Memory
{
    internal class MemoryTraderRepository : ITraderRepository, IHostedService
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, OrderQueryResult>> _orders = new();
        private readonly ConcurrentDictionary<string, long> _maxOrderIds = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, OrderQueryResult>> _transientOrders = new();

        private readonly ConcurrentDictionary<string, long> _lastPagedOrderId = new();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, AccountTrade>> _trades = new();
        private readonly ConcurrentDictionary<string, long> _maxTradeIds = new();

        #region Trader Repository

        public Task<long> GetLastPagedOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            if (_lastPagedOrderId.TryGetValue(symbol, out var orderId))
            {
                return Task.FromResult(orderId);
            }

            return Task.FromResult(0L);
        }

        public Task SetLastPagedOrderIdAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            _lastPagedOrderId[symbol] = orderId;

            return Task.CompletedTask;
        }

        public Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            if (_maxTradeIds.TryGetValue(symbol, out var value))
            {
                return Task.FromResult(value);
            }

            return Task.FromResult(0L);
        }

        public Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = long.MaxValue;

            if (_transientOrders.TryGetValue(symbol, out var lookup))
            {
                foreach (var item in lookup)
                {
                    if (item.Key < result)
                    {
                        result = item.Key;
                    }
                }
            }

            return Task.FromResult(result is long.MaxValue ? 0 : result);
        }

        public Task<OrderQueryResult?> GetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            if (_orders.TryGetValue(symbol, out var lookup) && lookup.TryGetValue(orderId, out var order))
            {
                return Task.FromResult<OrderQueryResult?>(order);
            }

            return Task.FromResult<OrderQueryResult?>(null);
        }

        public Task<SortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = new SortedOrderSet();

            if (_orders.TryGetValue(symbol, out var lookup))
            {
                foreach (var item in lookup)
                {
                    result.Add(item.Value);
                }
            }

            return Task.FromResult(result);
        }

        public Task<SortedTradeSet> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = new SortedTradeSet();

            if (_trades.TryGetValue(symbol, out var lookup))
            {
                foreach (var item in lookup)
                {
                    result.Add(item.Value);
                }
            }

            return Task.FromResult(result);
        }

        public Task<SortedOrderSet> GetTransientOrdersAsync(string symbol, OrderSide? orderSide = null, bool? significant = null, CancellationToken cancellationToken = default)
        {
            var result = new SortedOrderSet();

            if (_transientOrders.TryGetValue(symbol, out var lookup))
            {
                var query = lookup.AsEnumerable();

                if (orderSide.HasValue)
                {
                    query = query.Where(x => x.Value.Side == orderSide.Value);
                }

                if (significant.HasValue)
                {
                    if (significant.Value)
                    {
                        query = query.Where(x => x.Value.ExecutedQuantity > 0m);
                    }
                    else
                    {
                        query = query.Where(x => x.Value.ExecutedQuantity <= 0m);
                    }
                }

                foreach (var item in query)
                {
                    result.Add(item.Value);
                }
            }

            return Task.FromResult(result);
        }

        public Task<SortedOrderSet> GetTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            var result = new SortedOrderSet();

            if (_transientOrders.TryGetValue(symbol, out var lookup))
            {
                foreach (var item in lookup)
                {
                    if (item.Value.Side == orderSide)
                    {
                        result.Add(item.Value);
                    }
                }
            }

            return Task.FromResult(result);
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            if (order is null) throw new ArgumentNullException(nameof(order));

            AddOrUpdateOrder(order);

            return Task.CompletedTask;
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            if (orders is null) throw new ArgumentNullException(nameof(orders));

            foreach (var order in orders)
            {
                AddOrUpdateOrder(order);
            }

            return Task.CompletedTask;
        }

        public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
        {
            _ = trade ?? throw new ArgumentNullException(nameof(trade));

            _trades
                .GetOrAdd(trade.Symbol, _ => new ConcurrentDictionary<long, AccountTrade>())
                .AddOrUpdate(trade.Id, trade, (k, e) => trade);

            // update the max trade id index
            _maxTradeIds
                .AddOrUpdate(trade.Symbol, trade.Id, (key, current) => trade.Id > current ? trade.Id : current);

            return Task.CompletedTask;
        }

        public async Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            _ = trades ?? throw new ArgumentNullException(nameof(trades));

            foreach (var trade in trades)
            {
                await SetTradeAsync(trade, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task SetOrderAsync(CancelStandardOrderResult result, CancellationToken cancellationToken = default)
        {
            if (result is null) throw new ArgumentNullException(nameof(result));

            if (_orders.TryGetValue(result.Symbol, out var lookup) && lookup.TryGetValue(result.OrderId, out var item))
            {
                // mutate from the existing item
                var updated = item with
                {
                    ClientOrderId = result.ClientOrderId,
                    CummulativeQuoteQuantity = result.CummulativeQuoteQuantity,
                    ExecutedQuantity = result.ExecutedQuantity,
                    OrderListId = result.OrderListId,
                    OriginalQuantity = result.OriginalQuantity,
                    Price = result.Price,
                    Side = result.Side,
                    Status = result.Status,
                    TimeInForce = result.TimeInForce,
                    Type = result.Type
                };

                // update the store and the indexes
                AddOrUpdateOrder(updated);
            }

            return Task.CompletedTask;
        }

        public Task SetOrderAsync(OrderResult result, CancellationToken cancellationToken = default)
        {
            if (result is null) throw new ArgumentNullException(nameof(result));

            // todo: move this conversion to auto-mapper
            var order = new OrderQueryResult(
                result.Symbol,
                result.OrderId,
                result.OrderListId,
                result.ClientOrderId,
                result.Price,
                result.OriginalQuantity,
                result.ExecutedQuantity,
                result.CummulativeQuoteQuantity,
                result.Status,
                result.TimeInForce,
                result.Type,
                result.Side,
                0,
                0,
                result.TransactionTime,
                result.TransactionTime,
                true,
                0);

            AddOrUpdateOrder(order);

            return Task.CompletedTask;
        }

        private void AddOrUpdateOrder(OrderQueryResult order)
        {
            // add or update the main store
            _orders
                .GetOrAdd(order.Symbol, _ => new ConcurrentDictionary<long, OrderQueryResult>())
                .AddOrUpdate(order.OrderId, order, (key, current) => order);

            // update the max order id index
            _maxOrderIds.AddOrUpdate(order.Symbol, order.OrderId, (key, current) => order.OrderId > current ? order.OrderId : current);

            // update the transient order index
            if (order.Status.IsTransientStatus())
            {
                _transientOrders
                    .GetOrAdd(order.Symbol, _ => new ConcurrentDictionary<long, OrderQueryResult>())
                    .AddOrUpdate(order.OrderId, order, (key, current) => order);
            }
            else
            {
                _transientOrders
                    .GetOrAdd(order.Symbol, _ => new ConcurrentDictionary<long, OrderQueryResult>())
                    .TryRemove(order.OrderId, out _);
            }
        }

        #endregion Trader Repository

        #region Hosted Service

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        #endregion Hosted Service
    }
}