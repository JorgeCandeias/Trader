using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers.Trades;

public interface ITradeProviderReplicaGrain : IGrainWithStringKey
{
    ValueTask<TradeCollection> GetTradesAsync();

    ValueTask<AccountTrade?> TryGetTradeAsync(long tradeId);

    ValueTask SetTradeAsync(AccountTrade trade);

    ValueTask SetTradesAsync(IEnumerable<AccountTrade> trades);
}