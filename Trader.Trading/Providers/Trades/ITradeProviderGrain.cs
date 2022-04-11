namespace Outcompute.Trader.Trading.Providers.Trades;

internal interface ITradeProviderGrain : IGrainWithStringKey
{
    Task SetLastSyncedTradeIdAsync(long tradeId);

    Task<long> GetLastSyncedTradeIdAsync();

    Task<ReactiveResult> GetTradesAsync();

    Task<ReactiveResult?> TryWaitForTradesAsync(Guid version, int fromSerial);

    Task<AccountTrade?> TryGetTradeAsync(long tradeId);

    Task SetTradeAsync(AccountTrade trade);

    Task SetTradesAsync(IEnumerable<AccountTrade> trades);
}