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
        /// Returns all the cached klines.
        /// The result set is ordered by open time.
        /// </summary>
        Task<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the kline for the specified parameters.
        /// </summary>
        Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default);
    }
}