using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers;

public interface IKlineProvider
{
    /// <summary>
    /// Sets the last synced kline open time.
    /// </summary>
    Task SetLastSyncedKlineOpenTimeAsync(string symbol, KlineInterval interval, DateTime time, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the last synced kline open time.
    /// </summary>
    Task<DateTime> GetLastSyncedKlineOpenTimeAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all the cached klines.
    /// The result set is ordered by open time.
    /// </summary>
    ValueTask<KlineCollection> GetKlinesAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the last <paramref name="periods"/> cached klines up to and including <paramref name="tickTime"/>.
    /// </summary>
    ValueTask<KlineCollection> GetKlinesAsync(string symbol, KlineInterval interval, DateTime tickTime, int periods, CancellationToken cancellationToken = default);

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
    /// Publishes the specified kline using backpressure based conflation.
    /// The latest kline published for a given key will eventually be saved as soon as the system allows, with any interim versions being discarded.
    /// This method returns immediately and is designed to be called from an exchange streaming client.
    /// </summary>
    ValueTask ConflateKlineAsync(Kline item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last persisted open time for the specified parameters.
    /// </summary>
    ValueTask<DateTime?> TryGetLastOpenTimeAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default);
}