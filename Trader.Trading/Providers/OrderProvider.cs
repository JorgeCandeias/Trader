using AutoMapper;
using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        public async ValueTask<long> GetMaxOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var orders = await _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync().ConfigureAwait(false);

            return orders
                .Select(x => x.OrderId)
                .LastOrDefault();
        }

        public async ValueTask<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var orders = await _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync().ConfigureAwait(false);

            return orders
                .Where(x => x.Status.IsTransientStatus())
                .Select(x => x.OrderId)
                .FirstOrDefault();
        }

        public async ValueTask<IReadOnlyList<OrderQueryResult>> GetNonSignificantTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            var orders = await _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync().ConfigureAwait(false);

            return orders
                .Where(x => x.Side == orderSide && x.ExecutedQuantity <= 0m && x.Status.IsTransientStatus())
                .ToImmutableList();
        }

        public ValueTask<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync();
        }

        public async ValueTask<IReadOnlyList<OrderQueryResult>> GetSignificantCompletedOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var orders = await _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync().ConfigureAwait(false);

            return orders
                .Where(x => x.ExecutedQuantity > 0 && x.Status.IsCompletedStatus())
                .ToImmutableList();
        }

        public async ValueTask<IReadOnlyList<OrderQueryResult>> GetTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            var orders = await _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync().ConfigureAwait(false);

            return orders
                .Where(x => x.Side == orderSide && x.Status.IsTransientStatus())
                .ToImmutableList();
        }

        public ValueTask SetOrdersAsync(string symbol, IReadOnlyCollection<OrderQueryResult> items, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderGrain(symbol).SetOrdersAsync(items);
        }

        public ValueTask SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            return _factory.GetOrderProviderGrain(order.Symbol).SetOrderAsync(order);
        }

        public ValueTask SetOrderAsync(OrderResult order, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default)
        {
            var mapped = _mapper.Map<OrderQueryResult>(order, options =>
            {
                options.Items[nameof(OrderQueryResult.StopPrice)] = stopPrice;
                options.Items[nameof(OrderQueryResult.IcebergQuantity)] = icebergQuantity;
                options.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)] = originalQuoteOrderQuantity;
            });

            return SetOrderAsync(mapped, cancellationToken);
        }

        public async ValueTask SetOrderAsync(CancelStandardOrderResult order, CancellationToken cancellationToken = default)
        {
            var original = await _factory.GetOrderProviderGrain(order.Symbol).TryGetOrderAsync(order.OrderId);

            if (original is null)
            {
                throw new InvalidOperationException($"Unable to cancel order '{order.OrderId}' because its original could not be found");
            }

            var mapped = _mapper.Map<OrderQueryResult>(order, options =>
            {
                options.Items[nameof(OrderQueryResult.StopPrice)] = original.StopPrice;
                options.Items[nameof(OrderQueryResult.IcebergQuantity)] = original.IcebergQuantity;
                options.Items[nameof(OrderQueryResult.Time)] = original.Time;
                options.Items[nameof(OrderQueryResult.UpdateTime)] = original.UpdateTime.AddMilliseconds(1);
                options.Items[nameof(OrderQueryResult.IsWorking)] = original.IsWorking;
                options.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)] = original.OriginalQuoteOrderQuantity;
            });

            await SetOrderAsync(mapped, cancellationToken);
        }
    }
}