using AutoMapper;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal partial class TickerSynchronizer : ITickerSynchronizer
{
    private readonly ILogger _logger;
    private readonly ITickerProvider _tickers;
    private readonly ITradingService _trader;
    private readonly IMapper _mapper;

    public TickerSynchronizer(ILogger<TickerSynchronizer> logger, ITickerProvider tickers, ITradingService trader, IMapper mapper)
    {
        _logger = logger;
        _tickers = tickers;
        _trader = trader;
        _mapper = mapper;
    }

    private const string TypeName = nameof(TickerSynchronizer);

    public Task SyncAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        if (symbols is null) throw new ArgumentNullException(nameof(symbols));

        return SyncCoreAsync(symbols, cancellationToken);
    }

    private async Task SyncCoreAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        LogSyncingTickers(TypeName, symbols);

        var watch = Stopwatch.StartNew();

        // batches saving work in the background so we can keep pulling data without waiting
        var work = new ActionBlock<MiniTicker>(item => _tickers.SetTickerAsync(item, cancellationToken));

        // sync all symbols
        foreach (var symbol in symbols)
        {
            var subWatch = Stopwatch.StartNew();

            // get the current ticker from the exchange
            var result = await _trader
                .WithBackoff()
                .Get24hTickerPriceChangeStatisticsAsync(symbol, cancellationToken);

            // convert to the mini ticker that we use internally
            var ticker = _mapper.Map<MiniTicker>(result);

            // post for saving in the background
            work.Post(ticker);

            LogSyncedTicker(TypeName, symbol, subWatch.ElapsedMilliseconds);
        }

        // wait until all background saving work completes
        work.Complete();
        await work.Completion;

        LogSyncedTickers(TypeName, symbols, watch.ElapsedMilliseconds);
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} is syncing tickers for {Symbols}...")]
    private partial void LogSyncingTickers(string type, IEnumerable<string> symbols);

    [LoggerMessage(1, LogLevel.Information, "{Type} synced ticker for {Symbol} in {ElapsedMs}ms")]
    private partial void LogSyncedTicker(string type, string symbol, long elapsedMs);

    [LoggerMessage(2, LogLevel.Information, "{Type} synced tickers for {Symbols} in {ElapsedMs}ms")]
    private partial void LogSyncedTickers(string type, IEnumerable<string> symbols, long elapsedMs);

    #endregion Logging
}