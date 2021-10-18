using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IOrderProviderReplicaGrain : IGrainWithStringKey
    {
        ValueTask<IReadOnlyList<OrderQueryResult>> GetOrdersAsync();
    }
}