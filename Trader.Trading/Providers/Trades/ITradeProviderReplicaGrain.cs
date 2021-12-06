namespace Outcompute.Trader.Trading.Providers.Trades;

public interface ITradeProviderReplicaGrain : IGrainWithStringKey
{
    Task<ImmutableSortedSet<AccountTrade>> GetTradesAsync();

    Task<AccountTrade?> TryGetTradeAsync(long tradeId);

    Task SetTradeAsync(AccountTrade trade);

    Task SetTradesAsync(IEnumerable<AccountTrade> trades);
}