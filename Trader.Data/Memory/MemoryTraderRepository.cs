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
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, AccountTrade>> _trades = new();

        #region Trader Repository

        public Task<long> GetMaxOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = 0L;

            // todo: optimize this loop using an index
            if (_orders.TryGetValue(symbol, out var lookup))
            {
                foreach (var item in lookup)
                {
                    if (item.Key > result)
                    {
                        result = item.Key;
                    }
                }
            }

            return Task.FromResult(result);
        }

        public Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = 0L;

            // todo: optimize this loop using an index
            if (_trades.TryGetValue(symbol, out var lookup))
            {
                foreach (var item in lookup)
                {
                    if (item.Key > result)
                    {
                        result = item.Key;
                    }
                }
            }

            return Task.FromResult(result);
        }

        public Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = long.MaxValue;

            if (_orders.TryGetValue(symbol, out var lookup))
            {
                foreach (var item in lookup)
                {
                    if (item.Value.Status.IsTransientStatus() && item.Key < result)
                    {
                        result = item.Key;
                    }
                }
            }

            return Task.FromResult(result is long.MaxValue ? 0 : result);
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

        public Task<SortedTradeSet> GetTradesAsync(string symbol, long? orderId = null, CancellationToken cancellationToken = default)
        {
            var result = new SortedTradeSet();

            // todo: optimize this loop using an index
            if (_trades.TryGetValue(symbol, out var lookup))
            {
                foreach (var item in lookup)
                {
                    if (orderId.HasValue && item.Value.OrderId != orderId.Value) continue;

                    result.Add(item.Value);
                }
            }

            return Task.FromResult(result);
        }

        public Task<SortedOrderSet> GetTransientOrdersAsync(string symbol, OrderSide? orderSide = null, bool? significant = null, CancellationToken cancellationToken = default)
        {
            var result = new SortedOrderSet();

            // todo: optimize this loop using an index
            if (_orders.TryGetValue(symbol, out var lookup))
            {
                var query = lookup.Where(x => x.Value.Status.IsTransientStatus());

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

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            if (orders is null) throw new ArgumentNullException(nameof(orders));

            foreach (var order in orders)
            {
                var lookup = _orders.GetOrAdd(order.Symbol, _ => new ConcurrentDictionary<long, OrderQueryResult>());

                // todo: optimize this to cache the factory delegates
                lookup.AddOrUpdate(order.OrderId, order, (k, e) => order);
            }

            return Task.CompletedTask;
        }

        public Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            if (trades is null) throw new ArgumentNullException(nameof(trades));

            foreach (var trade in trades)
            {
                var lookup = _trades.GetOrAdd(trade.Symbol, _ => new ConcurrentDictionary<long, AccountTrade>());

                // todo: optimize this to cache the factory delegates
                lookup.AddOrUpdate(trade.Id, trade, (k, e) => trade);
            }

            return Task.CompletedTask;
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