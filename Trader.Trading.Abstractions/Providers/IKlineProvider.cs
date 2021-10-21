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

        /// <summary>
        /// Publishes the specified kline.
        /// </summary>
        Task SetKlineAsync(Kline item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the specified klines.
        /// </summary>
        Task SetKlinesAsync(string symbol, KlineInterval interval, IEnumerable<Kline> items, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the last persisted open time for the specified parameters.
        /// </summary>
        Task<DateTime?> TryGetLastOpenTimeAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default);
    }
}