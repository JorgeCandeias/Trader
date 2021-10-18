using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface IBinanceKlineProviderGrain : IGrainWithStringKey
    {
        Task<IReadOnlyCollection<Kline>> GetKlinesAsync(DateTime start, DateTime end);
    }
}