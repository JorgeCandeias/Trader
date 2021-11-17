using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers;

public static class SavingsProviderExtensions
{
    public static Task<SavingsBalance> GetBalanceOrZeroAsync(this ISavingsProvider provider, string asset, CancellationToken cancellationToken = default)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (asset is null) throw new ArgumentNullException(nameof(asset));

        return GetBalanceOrZeroCoreAsync(provider, asset, cancellationToken);

        async static Task<SavingsBalance> GetBalanceOrZeroCoreAsync(ISavingsProvider provider, string asset, CancellationToken cancellationToken)
        {
            return await provider.TryGetBalanceAsync(asset, cancellationToken).ConfigureAwait(false)
                ?? SavingsBalance.Zero(asset);
        }
    }

    public static Task<SavingsQuota> GetQuotaOrZeroAsync(this ISavingsProvider provider, string asset, CancellationToken cancellationToken = default)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (asset is null) throw new ArgumentNullException(nameof(asset));

        return GetQuotaOrZeroCoreAsync(provider, asset, cancellationToken);

        async static Task<SavingsQuota> GetQuotaOrZeroCoreAsync(ISavingsProvider provider, string asset, CancellationToken cancellationToken)
        {
            return await provider.TryGetQuotaAsync(asset, cancellationToken).ConfigureAwait(false)
                ?? SavingsQuota.Zero(asset);
        }
    }
}