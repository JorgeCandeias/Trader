using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Klines;

internal interface IKlineProviderGrain : IGrainWithStringKey
{
    ValueTask<ReactiveResult> GetKlinesAsync();

    ValueTask<ReactiveResult?> TryWaitForKlinesAsync(Guid version, int fromSerial);

    ValueTask<Kline?> TryGetKlineAsync(DateTime openTime);

    ValueTask SetKlineAsync(Kline item);

    ValueTask SetKlinesAsync(IEnumerable<Kline> items);
}