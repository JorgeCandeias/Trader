using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal interface IKlineProviderGrain : IGrainWithStringKey
    {
        Task<ReactiveResult> GetKlinesAsync();

        Task<ReactiveResult?> TryGetKlinesAsync(Guid version, int fromSerial);

        Task<Kline?> TryGetKlineAsync(DateTime openTime);

        Task SetKlineAsync(Kline item);

        Task SetKlinesAsync(IEnumerable<Kline> items);
    }
}