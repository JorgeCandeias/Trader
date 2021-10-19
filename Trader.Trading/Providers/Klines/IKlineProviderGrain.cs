using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal interface IKlineProviderGrain : IGrainWithStringKey
    {
        Task SetKlineAsync(Kline kline);

        Task SetKlinesAsync(IEnumerable<Kline> klines);

        Task<Kline?> TryGetKlineAsync(DateTime openTime);

        Task<(Guid Version, int MaxSerial, IReadOnlyList<Kline> Klines)> GetKlinesAsync();

        Task<(Guid Version, int MaxSerial, IReadOnlyList<Kline> Klines)> TryGetKlinesAsync(Guid version, int fromSerial);
    }
}