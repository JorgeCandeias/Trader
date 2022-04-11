namespace Outcompute.Trader.Trading.Providers.Swap;

internal class SwapPoolProvider : ISwapPoolProvider
{
    private readonly IGrainFactory _factory;

    public SwapPoolProvider(IGrainFactory factory)
    {
        _factory = factory;
    }

    public ValueTask<RedeemSwapPoolEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default)
    {
        return _factory.GetSwapPoolGrain().RedeemAsync(asset, amount);
    }

    public ValueTask<SwapPoolAssetBalance> GetBalanceAsync(string asset, CancellationToken cancellationToken = default)
    {
        return _factory.GetSwapPoolGrain().GetBalanceAsync(asset);
    }

    public ValueTask<IEnumerable<SwapPool>> GetSwapPoolsAsync(CancellationToken cancellationToken = default)
    {
        return _factory.GetSwapPoolGrain().GetSwapPoolsAsync();
    }

    public ValueTask<IEnumerable<SwapPoolConfiguration>> GetSwapPoolConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return _factory.GetSwapPoolGrain().GetSwapPoolConfigurationsAsync();
    }
}