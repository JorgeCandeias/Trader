using Orleans.Runtime;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Concurrent;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal partial class MarketDataStreamer : IMarketDataStreamer
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IMarketDataStreamClientFactory _streams;
    private readonly ITickerProvider _tickers;
    private readonly IKlineProvider _klines;
    private readonly IMarketDataStreamClient _client;

    public MarketDataStreamer(ILogger<MarketDataStreamer> logger, IMapper mapper, IMarketDataStreamClientFactory factory, ITickerProvider tickers, IKlineProvider klines)
    {
        _logger = logger;
        _mapper = mapper;
        _streams = factory;
        _tickers = tickers;
        _klines = klines;

        _client = _streams.Create(Array.Empty<string>());
    }

    private static string TypeName => nameof(MarketDataStreamer);

    private TaskCompletionSource _completion = new();

    public Task Completion => _completion.Task;

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "N/A")]
    public async Task StartAsync(IEnumerable<string> tickers, IEnumerable<(string Symbol, KlineInterval Interval)> klines, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(tickers, nameof(tickers));
        Guard.IsNotNull(klines, nameof(klines));

        // clear the old completion and issue a new one
        _completion.TrySetCanceled(CancellationToken.None);
        _completion = new();

        // attempt to start streaming
        try
        {
            var tickerLookup = tickers.ToHashSet();
            var klineLookup = klines.ToHashSet();

            // create a client for the streams we want
            var streams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            streams.UnionWith(tickerLookup.Select(x => $"{x.ToLowerInvariant()}@miniTicker"));
            streams.UnionWith(klineLookup.Select(x => $"{x.Symbol.ToLowerInvariant()}@kline_{_mapper.Map<string>(x.Interval)}"));

            LogConnectingToStreams(TypeName, streams);

            // connect to the socket
            await _client
                .ConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            // keeps track of command completion status so we dont overwhelm the exchange with commands
            var results = new ConcurrentDictionary<long, TaskCompletionSource>();

            // ensure we are receiving messages before subscribing to exchange streams
            var work = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var message = await _client
                            .ReceiveAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if (message.Error is not null)
                        {
                            throw new BinanceCodeException(message.Error.Code, message.Error.Message, 0);
                        }

                        if (message.MiniTicker is not null && tickerLookup.Contains(message.MiniTicker.Symbol))
                        {
                            await _tickers.ConflateTickerAsync(message.MiniTicker, cancellationToken).ConfigureAwait(false);
                        }

                        if (message.Kline is not null && klineLookup.Contains((message.Kline.Symbol, message.Kline.Interval)))
                        {
                            await _klines.ConflateKlineAsync(message.Kline, cancellationToken).ConfigureAwait(false);
                        }

                        if (message.Result is not null)
                        {
                            LogCommandResult(TypeName, message.Result.Id, message.Result.Result);

                            results[message.Result.Id].SetResult();
                        }
                    }
                }, cancellationToken);

            // subscribe to the exchange streams
            var id = 0;
            foreach (var batch in streams.BatchIEnumerable(10))
            {
                LogSubscribingToStreams(TypeName, batch);

                var completion = results[++id] = new();

                await _client
                    .SubscribeAsync(id, batch, cancellationToken)
                    .ConfigureAwait(false);

                await completion.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);

                LogSubscribedToStreams(TypeName, batch);
            }

            // link the background work to the completion exposed to the user
            work.TryLinkTo(_completion);
        }
        catch (Exception ex)
        {
            // any issue starting the stream also propagates to the completion
            _completion.TrySetException(ex);
            throw;
        }
    }

    [LoggerMessage(1, LogLevel.Information, "{Type} connecting to streams {Streams}")]
    private partial void LogConnectingToStreams(string type, IEnumerable<string> streams);

    [LoggerMessage(2, LogLevel.Error, "{Type} failed to push kline {Klines}")]
    private partial void LogFailedToPushKlines(Exception exception, string type, IEnumerable<Kline> klines);

    [LoggerMessage(3, LogLevel.Error, "{Type} failed to push ticker {Ticker}")]
    private partial void LogFailedToPushTicker(Exception exception, string type, MiniTicker ticker);

    [LoggerMessage(4, LogLevel.Information, "{Type} received command result {Id} as {Result}")]
    private partial void LogCommandResult(string type, long id, string result);

    [LoggerMessage(5, LogLevel.Information, "{Type} subscribing to stream {Streams}")]
    private partial void LogSubscribingToStreams(string type, IEnumerable<string> streams);

    [LoggerMessage(6, LogLevel.Information, "{Type} subscribed to stream {Streams}")]
    private partial void LogSubscribedToStreams(string type, IEnumerable<string> streams);
}