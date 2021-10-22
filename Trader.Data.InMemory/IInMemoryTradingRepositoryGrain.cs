using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Data.InMemory
{
    internal interface IInMemoryTradingRepositoryGrain : IGrainWithGuidKey
    {
        Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime);

        Task SetKlinesAsync(IEnumerable<Kline> items);

        Task SetKlineAsync(Kline item);

        Task<IEnumerable<OrderQueryResult>> GetOrdersAsync(string symbol);

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders);

        Task SetOrderAsync(OrderQueryResult order);

        Task SetTickerAsync(MiniTicker ticker);

        Task<MiniTicker?> TryGetTickerAsync(string symbol);
    }
}