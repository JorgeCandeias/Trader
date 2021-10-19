using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    internal class OrderProvider : IOrderProvider
    {
        private readonly IGrainFactory _factory;

        public OrderProvider(IGrainFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return await _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync();
        }

        public Task SetOrdersAsync(string symbol, IEnumerable<OrderQueryResult> items, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(symbol).SetOrdersAsync(items);
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(order.Symbol).SetOrderAsync(order);
        }

        public Task<OrderQueryResult?> TryGetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(symbol).TryGetOrderAsync(orderId);
        }
    }
}