using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Tests.Fakes
{
    internal interface IFakeTradingRepositoryGrain : IGrainWithGuidKey
    {
        Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime);

        Task SetKlinesAsync(IEnumerable<Kline> items);

        Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime);
    }
}