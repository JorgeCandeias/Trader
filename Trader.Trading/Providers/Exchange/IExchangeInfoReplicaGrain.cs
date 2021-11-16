using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Exchange;

internal interface IExchangeInfoReplicaGrain : IGrainWithGuidKey
{
    ValueTask<ExchangeInfo> GetExchangeInfoAsync();

    ValueTask<Symbol?> TryGetSymbolAsync(string name);
}