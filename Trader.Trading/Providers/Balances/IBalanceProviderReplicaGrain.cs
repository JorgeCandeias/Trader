using Orleans;
using Outcompute.Trader.Models;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Balances
{
    public interface IBalanceProviderReplicaGrain : IGrainWithStringKey
    {
        Task<Balance?> TryGetBalanceAsync();
    }
}