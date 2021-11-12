using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Data.InMemory
{
    internal interface IInMemoryTradingRepositoryGrain : IGrainWithGuidKey
    {
        ValueTask<ImmutableSortedSet<Kline>> GetKlinesAsync(string symbol, KlineInterval interval);

        ValueTask SetKlinesAsync(ImmutableList<Kline> items);

        ValueTask SetKlineAsync(Kline item);

        Task<ImmutableSortedSet<OrderQueryResult>> GetOrdersAsync(string symbol);

        Task SetOrdersAsync(ImmutableList<OrderQueryResult> orders);

        Task SetOrderAsync(OrderQueryResult order);

        Task SetTickerAsync(MiniTicker ticker);

        Task<MiniTicker?> TryGetTickerAsync(string symbol);

        Task<ImmutableSortedSet<AccountTrade>> GetTradesAsync(string symbol);

        Task SetTradeAsync(AccountTrade trade);

        Task SetTradesAsync(ImmutableList<AccountTrade> trades);

        ValueTask SetBalancesAsync(ImmutableList<Balance> balances);

        ValueTask<Balance?> TryGetBalanceAsync(string asset);
    }
}