using AutoMapper;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Core.Tasks.Dataflow;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal partial class MarketDataStreamer : IMarketDataStreamer
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IMarketDataStreamClientFactory _factory;
    private readonly IKlineProvider _klines;
    private readonly ITickerProvider _tickers;

    public MarketDataStreamer(ILogger<MarketDataStreamer> logger, IMapper mapper, IMarketDataStreamClientFactory factory, IKlineProvider klines, ITickerProvider tickers)
    {
        _logger = logger;
        _mapper = mapper;
        _factory = factory;
        _klines = klines;
        _tickers = tickers;
    }

    private static string TypeName => nameof(MarketDataStreamer);

    public Task StreamAsync(IEnumerable<string> tickers, IEnumerable<(string Symbol, KlineInterval Interval)> klines, CancellationToken cancellationToken = default)
    {
        return StreamCoreAsync(tickers, klines, cancellationToken);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "N/A")]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Worker")]
    private async Task StreamCoreAsync(IEnumerable<string> tickers, IEnumerable<(string Symbol, KlineInterval Interval)> klines, CancellationToken cancellationToken)
    {
        var tickerLookup = tickers.ToHashSet();
        var klineLookup = klines.ToHashSet();

        // create a client for the streams we want
        var streams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        streams.UnionWith(tickerLookup.Select(x => $"{x.ToLowerInvariant()}@miniTicker"));
        streams.UnionWith(klineLookup.Select(x => $"{x.Symbol.ToLowerInvariant()}@kline_{_mapper.Map<string>(x.Interval)}"));

        LogConnectingToStreams(TypeName, streams);

        using var client = _factory.Create(streams);

        await client
            .ConnectAsync(cancellationToken)
            .ConfigureAwait(false);

        // this worker action pushes incoming klines to the system in the background so we dont hold up the binance stream
        var klineWorkers = new Dictionary<(string Symbol, KlineInterval Interval), BackpressureActionBlock<Kline>>();

        // this worker action pushes incoming tickers to the system in the background so we dont hold up the binance stream
        var tickerWorkers = new Dictionary<string, BackpressureActionBlock<MiniTicker>>();

        // now we can stream from the exchange
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await client
                .ReceiveAsync(cancellationToken)
                .ConfigureAwait(false);

            if (message.Error is not null)
            {
                throw new BinanceCodeException(message.Error.Code, message.Error.Message, 0);
            }

            if (message.MiniTicker is not null && tickerLookup.Contains(message.MiniTicker.Symbol))
            {
                if (!tickerWorkers.TryGetValue(message.MiniTicker.Symbol, out var worker))
                {
                    tickerWorkers[message.MiniTicker.Symbol] = worker = new BackpressureActionBlock<MiniTicker>(async items =>
                    {
                        // keep the latest ticker only
                        var item = items.MaxBy(x => x.EventTime)!;

                        try
                        {
                            await _tickers.SetTickerAsync(item, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            LogFailedToPushTicker(ex, TypeName, item);
                        }
                    });
                }

                worker.Post(message.MiniTicker);
            }

            if (message.Kline is not null && klineLookup.Contains((message.Kline.Symbol, message.Kline.Interval)))
            {
                if (!klineWorkers.TryGetValue((message.Kline.Symbol, message.Kline.Interval), out var worker))
                {
                    klineWorkers[(message.Kline.Symbol, message.Kline.Interval)] = worker = new BackpressureActionBlock<Kline>(async items =>
                    {
                        // keep the latest kline for each open time only
                        var conflated = items
                            .GroupBy(x => x.OpenTime)
                            .Select(x => x.MaxBy(x => x.EventTime)!)
                            .ToImmutableList();

                        try
                        {
                            await _klines.SetKlinesAsync(message.Kline.Symbol, message.Kline.Interval, conflated, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            LogFailedToPushKlines(ex, TypeName, conflated);
                        }
                    });
                }

                worker.Post(message.Kline);
            }
        }
    }

    [LoggerMessage(0, LogLevel.Information, "{Type} connecting to streams {Streams}")]
    private partial void LogConnectingToStreams(string type, IEnumerable<string> streams);

    [LoggerMessage(1, LogLevel.Error, "{Type} failed to push kline {Klines}")]
    private partial void LogFailedToPushKlines(Exception exception, string type, IEnumerable<Kline> klines);

    [LoggerMessage(2, LogLevel.Error, "{Type} failed to push ticker {Ticker}")]
    private partial void LogFailedToPushTicker(Exception exception, string type, MiniTicker ticker);
}