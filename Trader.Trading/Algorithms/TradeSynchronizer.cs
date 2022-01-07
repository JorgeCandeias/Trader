using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Providers;
using System.Diagnostics;

namespace Outcompute.Trader.Trading.Algorithms;

internal partial class TradeSynchronizer : ITradeSynchronizer
{
    private readonly ILogger _logger;
    private readonly ITradingService _trader;
    private readonly ITradeProvider _provider;

    public TradeSynchronizer(ILogger<TradeSynchronizer> logger, ITradingService trader, ITradeProvider provider)
    {
        _logger = logger;
        _trader = trader;
        _provider = provider;
    }

    private static string TypeName { get; } = nameof(TradeSynchronizer);

    public async Task SynchronizeTradesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();

        // start from the last synced trade
        var tradeId = await _provider.GetLastSyncedTradeIdAsync(symbol, cancellationToken);

        // pull all trades
        var count = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            // query for the next trades
            var trades = await _trader.WithBackoff().GetAccountTradesAsync(symbol, tradeId, 1000, cancellationToken);

            // break if we got all trades
            if (trades.Count is 0) break;

            // persist all new trades in this page
            await _provider.SetTradesAsync(symbol, trades, cancellationToken);

            // set the last synced trade id so we continue from that next time
            await _provider.SetLastSyncedTradeIdAsync(symbol, trades.Max!.Id, cancellationToken);

            // setup the next loop
            tradeId = trades.Max!.Id + 1;

            // keep track for logging
            count += trades.Count;

            // break if we got a partial page
            if (trades.Count < 900) break;
        }

        LogPulledTrades(TypeName, symbol, count, tradeId, watch.ElapsedMilliseconds);
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} {Symbol} pulled {Count} trades up to TradeId {MaxTradeId} in {ElapsedMs}ms")]
    private partial void LogPulledTrades(string type, string symbol, int count, long maxTradeId, long elapsedMs);

    #endregion Logging
}