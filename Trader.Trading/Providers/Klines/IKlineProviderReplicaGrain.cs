using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    public interface IKlineProviderReplicaGrain : IGrainWithStringKey
    {
        Task<IReadOnlyList<Kline>> GetKlinesAsync();

        Task<Kline?> TryGetKlineAsync(DateTime openTime);
    }
}