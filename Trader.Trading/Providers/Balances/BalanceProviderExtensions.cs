using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public static class BalanceProviderExtensions
    {
        public static Task<Balance> GetBalanceOrZeroAsync(this IBalanceProvider provider, string asset, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            return GetBalanceOrZeroCoreAsync(provider, asset, cancellationToken);

            async static Task<Balance> GetBalanceOrZeroCoreAsync(IBalanceProvider provider, string asset, CancellationToken cancellationToken)
            {
                return await provider.TryGetBalanceAsync(asset, cancellationToken).ConfigureAwait(false)
                    ?? Balance.Zero(asset);
            }
        }
    }
}