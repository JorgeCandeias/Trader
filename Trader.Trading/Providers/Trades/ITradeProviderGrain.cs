using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Trades;

internal interface ITradeProviderGrain : IGrainWithStringKey
{
    Task SetLastSyncedTradeIdAsync(long tradeId);

    ValueTask<long> GetLastSyncedTradeIdAsync();

    ValueTask<ReactiveResult> GetTradesAsync();

    ValueTask<ReactiveResult?> TryWaitForTradesAsync(Guid version, int fromSerial);

    ValueTask<AccountTrade?> TryGetTradeAsync(long tradeId);

    ValueTask SetTradeAsync(AccountTrade trade);

    ValueTask SetTradesAsync(IEnumerable<AccountTrade> trades);
}