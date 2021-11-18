namespace Outcompute.Trader.Trading.Providers;

public static class TradeProviderExtensions
{
    // todo: move this logic effort to the replica grain
    public static Task<long?> TryGetLastTradeIdAsync(this ITradeProvider provider, string symbol, CancellationToken cancellationToken = default)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return provider.TryGetLastTradeIdCoreAsync(symbol, cancellationToken);
    }

    private static async Task<long?> TryGetLastTradeIdCoreAsync(this ITradeProvider provider, string symbol, CancellationToken cancellationToken = default)
    {
        var trades = await provider
            .GetTradesAsync(symbol, cancellationToken)
            .ConfigureAwait(false);

        return trades.Count > 0 ? trades[trades.Count - 1].Id : null;
    }
}