using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IKlineProvider
    {
        Task<IEnumerable<Kline>> TryGetTickerAsync(string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    }
}