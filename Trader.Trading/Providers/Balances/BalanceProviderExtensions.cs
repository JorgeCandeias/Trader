using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public static class BalanceProviderExtensions
    {
        public static ValueTask<Balance> GetBalanceOrZeroAsync(this IBalanceProvider provider, string asset, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            return GetBalanceOrZeroCoreAsync(provider, asset, cancellationToken);

            async static ValueTask<Balance> GetBalanceOrZeroCoreAsync(IBalanceProvider provider, string asset, CancellationToken cancellationToken)
            {
                return await provider.TryGetBalanceAsync(asset, cancellationToken).ConfigureAwait(false)
                    ?? Balance.Zero(asset);
            }
        }

        public static ValueTask<Balance> GetRequiredBalanceAsync(this IBalanceProvider provider, string asset, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            return GetRequiredBalanceCoreAsync(provider, asset, cancellationToken);

            async static ValueTask<Balance> GetRequiredBalanceCoreAsync(IBalanceProvider provider, string asset, CancellationToken cancellationToken)
            {
                return await provider.TryGetBalanceAsync(asset, cancellationToken).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Balance not found for asset '{asset}'");
            }
        }
    }
}