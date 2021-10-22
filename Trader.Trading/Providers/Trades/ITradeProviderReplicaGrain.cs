using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Trades
{
    public interface ITradeProviderReplicaGrain : IGrainWithStringKey
    {
        Task<IReadOnlyList<AccountTrade>> GetTradesAsync();

        Task<AccountTrade?> TryGetTradeAsync(long tradeId);

        Task SetTradeAsync(AccountTrade trade);

        Task SetTradesAsync(IEnumerable<AccountTrade> trades);
    }
}