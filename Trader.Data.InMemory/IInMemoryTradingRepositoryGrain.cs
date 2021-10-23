using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Data.InMemory
{
    internal interface IInMemoryTradingRepositoryGrain : IGrainWithGuidKey
    {
        Task<ImmutableSortedSet<Kline>> GetKlinesAsync(string symbol, KlineInterval interval);

        Task SetKlinesAsync(ImmutableList<Kline> items);

        Task SetKlineAsync(Kline item);

        Task<ImmutableSortedSet<OrderQueryResult>> GetOrdersAsync(string symbol);

        Task SetOrdersAsync(ImmutableList<OrderQueryResult> orders);

        Task SetOrderAsync(OrderQueryResult order);

        Task SetTickerAsync(MiniTicker ticker);

        Task<MiniTicker?> TryGetTickerAsync(string symbol);

        Task<ImmutableSortedSet<AccountTrade>> GetTradesAsync(string symbol);

        Task SetTradeAsync(AccountTrade trade);

        Task SetTradesAsync(ImmutableList<AccountTrade> trades);

        Task SetBalancesAsync(ImmutableList<Balance> balances);

        Task<Balance?> TryGetBalanceAsync(string asset);
    }
}