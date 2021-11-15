using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IKlineProvider
    {
        /// <summary>
        /// Returns all the cached klines.
        /// The result set is ordered by open time.
        /// </summary>
        ValueTask<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the last <paramref name="periods"/> cached klines up to and including <paramref name="tickTime"/>.
        /// </summary>
        ValueTask<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime tickTime, int periods, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the kline for the specified parameters.
        /// </summary>
        ValueTask<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the specified kline.
        /// </summary>
        ValueTask SetKlineAsync(Kline item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the specified klines.
        /// </summary>
        ValueTask SetKlinesAsync(string symbol, KlineInterval interval, IEnumerable<Kline> items, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the last persisted open time for the specified parameters.
        /// </summary>
        ValueTask<DateTime?> TryGetLastOpenTimeAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default);
    }
}