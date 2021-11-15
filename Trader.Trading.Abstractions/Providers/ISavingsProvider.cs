using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ISavingsProvider
    {
        ValueTask<IReadOnlyList<SavingsProduct>> GetProductsAsync(CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<SavingsPosition>> GetPositionsAsync(CancellationToken cancellationToken = default);

        ValueTask<SavingsPosition?> TryGetPositionAsync(string asset, CancellationToken cancellation = default);

        ValueTask<SavingsQuota?> TryGetQuotaAsync(string asset, CancellationToken cancellationToken = default);

        ValueTask<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
    }
}