using AutoMapper;
using Orleans;
using Orleans.Runtime;
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
        internal static ILocalSiloDetails LocalSiloDetails { get; set; } = null!;

        public static Task<long> GetMaxOrderIdAsync(this IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            _ = provider ?? throw new ArgumentNullException(nameof(provider));

            return provider.GetMaxOrderIdCoreAsync(symbol, cancellationToken);
        }

        private static async Task<long> GetMaxOrderIdCoreAsync(this IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

            return orders
                .Select(x => x.OrderId)
                .LastOrDefault();
        }

        public static Task<long> GetMinTransientOrderIdAsync(this IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            _ = provider ?? throw new ArgumentNullException(nameof(provider));

            return provider.GetMinTransientOrderIdCoreAsync(symbol, cancellationToken);
        }

        private static async Task<long> GetMinTransientOrderIdCoreAsync(this IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

            return orders
                .Where(x => x.Status.IsTransientStatus())
                .Select(x => x.OrderId)
                .FirstOrDefault();
        }

        public static Task<IReadOnlyList<OrderQueryResult>> GetNonSignificantTransientOrdersBySideAsync(this IOrderProvider provider, string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            _ = provider ?? throw new ArgumentNullException(nameof(provider));

            return provider.GetNonSignificantTransientOrdersBySideCoreAsync(symbol, orderSide, cancellationToken);
        }

        private static async Task<IReadOnlyList<OrderQueryResult>> GetNonSignificantTransientOrdersBySideCoreAsync(this IOrderProvider provider, string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

            return orders
                .Where(x => x.Side == orderSide && x.ExecutedQuantity <= 0m && x.Status.IsTransientStatus())
                .ToImmutableList();
        }

        public static Task<IReadOnlyList<OrderQueryResult>> GetSignificantCompletedOrdersAsync(this IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            _ = provider ?? throw new ArgumentNullException(nameof(provider));

            return provider.GetSignificantCompletedOrdersCoreAsync(symbol, cancellationToken);
        }

        private static async Task<IReadOnlyList<OrderQueryResult>> GetSignificantCompletedOrdersCoreAsync(this IOrderProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

            return orders
                .Where(x => x.ExecutedQuantity > 0 && x.Status.IsCompletedStatus())
                .ToImmutableList();
        }

        public static Task<IReadOnlyList<OrderQueryResult>> GetTransientOrdersBySideAsync(this IOrderProvider provider, string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            _ = provider ?? throw new ArgumentNullException(nameof(provider));

            return provider.GetTransientOrdersBySideCoreAsync(symbol, orderSide, cancellationToken);
        }

        private static async Task<IReadOnlyList<OrderQueryResult>> GetTransientOrdersBySideCoreAsync(this IOrderProvider provider, string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            var orders = await provider.GetOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

            return orders
                .Where(x => x.Side == orderSide && x.Status.IsTransientStatus())
                .ToImmutableList();
        }

        public static Task SetOrderAsync(this IOrderProvider provider, OrderResult order, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default)
        {
            _ = provider ?? throw new ArgumentNullException(nameof(provider));

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
            _ = provider ?? throw new ArgumentNullException(nameof(provider));
            _ = order ?? throw new ArgumentNullException(nameof(order));

            return provider.SetOrderCoreAsync(order, cancellationToken);
        }

        private static async Task SetOrderCoreAsync(this IOrderProvider provider, CancelStandardOrderResult order, CancellationToken cancellationToken = default)
        {
            var original = await GrainFactory
                .GetOrderProviderReplicaGrain(LocalSiloDetails.SiloAddress, order.Symbol)
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