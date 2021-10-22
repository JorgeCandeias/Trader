using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    internal class OrderProvider : IOrderProvider
    {
        private readonly IGrainFactory _factory;
        private readonly ITradingRepository _repository;

        public OrderProvider(IGrainFactory factory, ITradingRepository repository)
        {
            _factory = factory;
            _repository = repository;
        }

        public async Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return await _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync();
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            if (order is null) throw new ArgumentNullException(nameof(order));

            return SetOrderCoreAsync(order, cancellationToken);
        }

        private async Task SetOrderCoreAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            await _repository
                .SetOrderAsync(order, cancellationToken)
                .ConfigureAwait(false);

            await _factory
                .GetOrderProviderReplicaGrain(order.Symbol)
                .SetOrderAsync(order);
        }

        public Task<OrderQueryResult?> TryGetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(symbol).TryGetOrderAsync(orderId);
        }

        public Task SetOrdersAsync(string symbol, IEnumerable<OrderQueryResult> items, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));
            if (items is null) throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                if (item.Symbol != symbol) throw new ArgumentOutOfRangeException(nameof(items), $"Order has symbol '{item.Symbol}' different from partition symbol '{symbol}'");
            }

            return SetOrdersCoreAsync(symbol, items, cancellationToken);
        }

        private async Task SetOrdersCoreAsync(string symbol, IEnumerable<OrderQueryResult> items, CancellationToken cancellationToken = default)
        {
            await _repository
                .SetOrdersAsync(items, cancellationToken)
                .ConfigureAwait(false);

            await _factory
                .GetOrderProviderReplicaGrain(symbol)
                .SetOrdersAsync(items)
                .ConfigureAwait(false);
        }
    }
}