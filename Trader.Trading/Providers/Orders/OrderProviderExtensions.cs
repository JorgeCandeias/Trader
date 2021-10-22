using AutoMapper;
using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    public static class OrderProviderExtensions
    {
        internal static IMapper Mapper { get; set; } = null!;
        internal static IGrainFactory GrainFactory { get; set; } = null!;

        public static Task<long?> TryGetMaxOrderIdAsync(this IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            return TryGetMaxOrderIdCoreAsync(provider, symbol, cancellationToken);

            static async Task<long?> TryGetMaxOrderIdCoreAsync(IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
            {
                var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

                return orders
                    .Select(x => (long?)x.OrderId)
                    .LastOrDefault();
            }
        }

        public static Task<long?> TryGetMinTransientOrderIdAsync(this IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            return GetMinTransientOrderIdCoreAsync(provider, symbol, cancellationToken);

            static async Task<long?> GetMinTransientOrderIdCoreAsync(IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
            {
                var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

                foreach (var item in orders)
                {
                    if (item.Status.IsTransientStatus())
                    {
                        return item.OrderId;
                    }
                }

                return null;
            }
        }

        public static Task<IReadOnlyList<OrderQueryResult>> GetNonSignificantTransientOrdersBySideAsync(this IOrderProvider provider, string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            return GetNonSignificantTransientOrdersBySideCoreAsync(provider, symbol, orderSide, cancellationToken);

            static async Task<IReadOnlyList<OrderQueryResult>> GetNonSignificantTransientOrdersBySideCoreAsync(IOrderProvider provider, string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
            {
                var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

                return orders
                    .Where(x => x.Side == orderSide && x.ExecutedQuantity <= 0m && x.Status.IsTransientStatus())
                    .ToImmutableList();
            }
        }

        public static Task<IReadOnlyList<OrderQueryResult>> GetSignificantCompletedOrdersAsync(this IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            return GetSignificantCompletedOrdersCoreAsync(provider, symbol, cancellationToken);

            static async Task<IReadOnlyList<OrderQueryResult>> GetSignificantCompletedOrdersCoreAsync(IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
            {
                var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

                return orders
                    .Where(x => x.ExecutedQuantity > 0 && x.Status.IsCompletedStatus())
                    .ToImmutableList();
            }
        }

        public static Task<IReadOnlyList<OrderQueryResult>> GetTransientOrdersBySideAsync(this IOrderProvider provider, string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            return GetTransientOrdersBySideCoreAsync(provider, symbol, orderSide, cancellationToken);

            static async Task<IReadOnlyList<OrderQueryResult>> GetTransientOrdersBySideCoreAsync(IOrderProvider provider, string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
            {
                var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

                return orders
                    .Where(x => x.Side == orderSide && x.Status.IsTransientStatus())
                    .ToImmutableList();
            }
        }

        public static Task SetOrderAsync(this IOrderProvider provider, OrderResult order, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            var mapped = Mapper.Map<OrderQueryResult>(order, options =>
            {
                options.Items[nameof(OrderQueryResult.StopPrice)] = stopPrice;
                options.Items[nameof(OrderQueryResult.IcebergQuantity)] = icebergQuantity;
                options.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)] = originalQuoteOrderQuantity;
            });

            return provider.SetOrderAsync(mapped, cancellationToken);
        }

        public static Task SetOrderAsync(this IOrderProvider provider, CancelStandardOrderResult order, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));
            if (order is null) throw new ArgumentNullException(nameof(order));

            return SetOrderCoreAsync(provider, order, cancellationToken);

            static async Task SetOrderCoreAsync(IOrderProvider provider, CancelStandardOrderResult order, CancellationToken cancellationToken = default)
            {
                var original = await GrainFactory
                    .GetOrderProviderReplicaGrain(order.Symbol)
                    .TryGetOrderAsync(order.OrderId)
                    .ConfigureAwait(false);

                if (original is null)
                {
                    throw new InvalidOperationException($"Unable to cancel order '{order.OrderId}' because its original could not be found");
                }

                var mapped = Mapper.Map<OrderQueryResult>(order, options =>
                {
                    options.Items[nameof(OrderQueryResult.StopPrice)] = original.StopPrice;
                    options.Items[nameof(OrderQueryResult.IcebergQuantity)] = original.IcebergQuantity;
                    options.Items[nameof(OrderQueryResult.Time)] = original.Time;
                    options.Items[nameof(OrderQueryResult.UpdateTime)] = original.UpdateTime;
                    options.Items[nameof(OrderQueryResult.IsWorking)] = original.IsWorking;
                    options.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)] = original.OriginalQuoteOrderQuantity;
                });

                await provider
                    .SetOrderAsync(mapped, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}