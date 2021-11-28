using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers.Trades;

public interface ITradeProviderReplicaGrain : IGrainWithStringKey
{
    Task<TradeCollection> GetTradesAsync();

    Task<AccountTrade?> TryGetTradeAsync(long tradeId);

    Task SetTradeAsync(AccountTrade trade);

    Task SetTradesAsync(IEnumerable<AccountTrade> trades);
}