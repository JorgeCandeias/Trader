namespace Outcompute.Trader.Trading.Providers.Savings;

internal class SavingsProvider : ISavingsProvider
{
    private readonly IGrainFactory _factory;

    public SavingsProvider(IGrainFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public ValueTask<IReadOnlyList<SavingsProduct>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return _factory.GetSavingsGrain().GetProductsAsync();
    }

    public ValueTask<IEnumerable<SavingsBalance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        return _factory.GetSavingsGrain().GetBalancesAsync();
    }

    public ValueTask<SavingsBalance?> TryGetBalanceAsync(string asset, CancellationToken cancellation = default)
    {
        return _factory.GetSavingsGrain().TryGetBalanceAsync(asset);
    }

    public ValueTask<SavingsQuota?> TryGetQuotaAsync(string asset, CancellationToken cancellationToken = default)
    {
        return _factory.GetSavingsGrain().TryGetQuotaAsync(asset);
    }

    public ValueTask<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default)
    {
        return _factory.GetSavingsGrain().RedeemAsync(asset, amount);
    }
}