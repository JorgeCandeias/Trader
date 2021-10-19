using Orleans;
using Outcompute.Trader.Models;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Exchange
{
    internal interface IExchangeInfoReplicaGrain : IGrainWithGuidKey
    {
        Task<ExchangeInfo> GetExchangeInfoAsync();

        Task<Symbol?> TryGetSymbolAsync(string name);
    }
}