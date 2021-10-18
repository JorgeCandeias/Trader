using AutoMapper;
using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    internal class OrderProvider : IOrderProvider
    {
        private readonly IGrainFactory _factory;
        private readonly IMapper _mapper;

        public OrderProvider(IGrainFactory factory, IMapper mapper)
        {
            _factory = factory;
            _mapper = mapper;
        }

        public Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync();
        }

        public Task SetOrdersAsync(string symbol, IReadOnlyCollection<OrderQueryResult> items, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(symbol).SetOrdersAsync(items);
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(order.Symbol).SetOrderAsync(order);
        }
    }
}