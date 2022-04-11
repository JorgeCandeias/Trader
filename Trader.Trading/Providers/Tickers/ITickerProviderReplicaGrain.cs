namespace Outcompute.Trader.Trading.Providers.Tickers;

public interface ITickerProviderReplicaGrain : IGrainWithStringKey
{
    Task<MiniTicker?> TryGetTickerAsync();

    Task SetTickerAsync(MiniTicker item);
}