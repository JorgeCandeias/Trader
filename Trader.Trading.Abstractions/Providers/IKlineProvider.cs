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
        /// Saves the specifed klines under the specified symbol and interval.
        /// </summary>
        Task SetKlinesAsync(string symbol, KlineInterval interval, IEnumerable<Kline> items, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the specified kline.
        /// </summary>
        Task SetKlineAsync(Kline kline, CancellationToken cancellationToken = default);

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