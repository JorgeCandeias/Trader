using Orleans;
using Orleans.Runtime;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    internal class OrderProvider : IOrderProvider
    {
        private readonly IGrainFactory _factory;
        private readonly ILocalSiloDetails _details;

        public OrderProvider(IGrainFactory factory, ILocalSiloDetails details)
        {
            _factory = factory;
            _details = details;
        }

        public async Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return await _factory.GetOrderProviderReplicaGrain(_details.SiloAddress, symbol).GetOrdersAsync();
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(_details.SiloAddress, order.Symbol).SetOrderAsync(order);
        }

        public Task<OrderQueryResult?> TryGetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(_details.SiloAddress, symbol).TryGetOrderAsync(orderId);
        }
    }
}