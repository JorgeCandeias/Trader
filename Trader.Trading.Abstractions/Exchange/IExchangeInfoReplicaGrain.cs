using Orleans;
using Outcompute.Trader.Models;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Exchange
{
    public interface IExchangeInfoReplicaGrain : IGrainWithGuidKey
    {
        ValueTask<ExchangeInfo> GetExchangeInfoAsync();

        ValueTask<Symbol?> TryGetSymbolAsync(string name);
    }
}