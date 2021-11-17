using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers;

public interface ISavingsProvider
{
    ValueTask<IReadOnlyList<SavingsProduct>> GetProductsAsync(CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<SavingsBalance>> GetBalancesAsync(CancellationToken cancellationToken = default);

    ValueTask<SavingsBalance?> TryGetBalanceAsync(string asset, CancellationToken cancellation = default);

    ValueTask<SavingsQuota?> TryGetQuotaAsync(string asset, CancellationToken cancellationToken = default);

    ValueTask<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
}