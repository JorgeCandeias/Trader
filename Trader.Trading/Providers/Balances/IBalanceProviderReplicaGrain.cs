using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Balances;

public interface IBalanceProviderReplicaGrain : IGrainWithStringKey
{
    ValueTask<Balance?> TryGetBalanceAsync();
}