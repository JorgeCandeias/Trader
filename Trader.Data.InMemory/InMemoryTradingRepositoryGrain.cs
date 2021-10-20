using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Data.InMemory
{
    [Reentrant]
    internal class InMemoryTradingRepositoryGrain : Grain, IInMemoryTradingRepositoryGrain
    {
        private readonly Dictionary<string, ImmutableSortedSet<OrderQueryResult>.Builder> _orders = new();
        private readonly Dictionary<(string Symbol, KlineInterval Interval, DateTime OpenTime), Kline> _klines = new();

        public Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var result = _klines.Values
                .Where(x => x.Symbol == symbol && x.Interval == interval && x.OpenTime >= startOpenTime && x.OpenTime <= endOpenTime)
                .ToImmutableList();

            return Task.FromResult<IEnumerable<Kline>>(result);
        }

        public Task SetKlinesAsync(IEnumerable<Kline> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                _klines[(item.Symbol, item.Interval, item.OpenTime)] = item;
            }

            return Task.CompletedTask;
        }

        public Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            if (_klines.TryGetValue((symbol, interval, openTime), out var value))
            {
                return Task.FromResult<Kline?>(value);
            }

            return Task.FromResult<Kline?>(null);
        }

        public Task SetKlineAsync(Kline item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            _klines[(item.Symbol, item.Interval, item.OpenTime)] = item;

            return Task.CompletedTask;
        }

        public Task<IEnumerable<OrderQueryResult>> GetOrdersAsync(string symbol)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var result = _orders.TryGetValue(symbol, out var orders)
                ? orders.ToImmutable()
                : ImmutableSortedSet<OrderQueryResult>.Empty;

            return Task.FromResult<IEnumerable<OrderQueryResult>>(result);
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders)
        {
            if (orders is null) throw new ArgumentNullException(nameof(orders));

            foreach (var order in orders)
            {
                SetOrderCore(order);
            }

            return Task.CompletedTask;
        }

        public Task SetOrderAsync(OrderQueryResult order)
        {
            if (order is null) throw new ArgumentNullException(nameof(order));

            SetOrderCore(order);

            return Task.CompletedTask;
        }

        private void SetOrderCore(OrderQueryResult order)
        {
            if (!_orders.TryGetValue(order.Symbol, out var builder))
            {
                _orders[order.Symbol] = builder = ImmutableSortedSet.CreateBuilder(OrderQueryResult.OrderIdComparer);
            }

            builder.Remove(order);
            builder.Add(order);
        }
    }
}