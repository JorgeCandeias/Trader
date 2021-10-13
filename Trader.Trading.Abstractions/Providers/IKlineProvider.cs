using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IKlineProvider
    {
        /// <summary>
        /// Returns the set of klines that fullfil the given criteria.
        /// Returns an empty or partial collection if no complete dataset exists.
        /// The result set is ordered by open time.
        /// </summary>
        ValueTask<IReadOnlyCollection<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    }
}