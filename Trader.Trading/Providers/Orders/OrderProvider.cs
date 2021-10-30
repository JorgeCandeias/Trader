using AutoMapper;
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
        private readonly IMapper _mapper;

        public OrderProvider(IGrainFactory factory, ITradingRepository repository, IMapper mapper)
        {
            _factory = factory;
            _repository = repository;
            _mapper = mapper;
        }

        public Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync();
        }

        public Task<IReadOnlyList<OrderQueryResult>> GetOrdersByFilterAsync(string symbol, OrderSide? side, bool? transient, bool? significant, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersByFilterAsync(side, transient, significant);
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

        public Task SetOrderAsync(OrderResult order, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default)
        {
            if (order is null) throw new ArgumentNullException(nameof(order));

            var mapped = _mapper.Map<OrderQueryResult>(order, options =>
            {
                options.Items[nameof(OrderQueryResult.StopPrice)] = stopPrice;
                options.Items[nameof(OrderQueryResult.IcebergQuantity)] = icebergQuantity;
                options.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)] = originalQuoteOrderQuantity;
            });

            return SetOrderAsync(mapped, cancellationToken);
        }

        public Task SetOrderAsync(CancelStandardOrderResult order, CancellationToken cancellationToken = default)
        {
            if (order is null) throw new ArgumentNullException(nameof(order));

            return SetOrderCoreAsync(order, cancellationToken);
        }

        private async Task SetOrderCoreAsync(CancelStandardOrderResult order, CancellationToken cancellationToken = default)
        {
            var original = await _factory
                .GetOrderProviderReplicaGrain(order.Symbol)
                .TryGetOrderAsync(order.OrderId)
                .ConfigureAwait(false);

            if (original is null)
            {
                throw new InvalidOperationException($"Unable to cancel order '{order.OrderId}' because its original could not be found");
            }

            var mapped = _mapper.Map<OrderQueryResult>(order, options =>
            {
                options.Items[nameof(OrderQueryResult.StopPrice)] = original.StopPrice;
                options.Items[nameof(OrderQueryResult.IcebergQuantity)] = original.IcebergQuantity;
                options.Items[nameof(OrderQueryResult.Time)] = original.Time;
                options.Items[nameof(OrderQueryResult.UpdateTime)] = original.UpdateTime;
                options.Items[nameof(OrderQueryResult.IsWorking)] = original.IsWorking;
                options.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)] = original.OriginalQuoteOrderQuantity;
            });

            await SetOrderAsync(mapped, cancellationToken).ConfigureAwait(false);
        }
    }
}