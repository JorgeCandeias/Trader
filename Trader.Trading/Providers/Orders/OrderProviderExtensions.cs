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
    }
}