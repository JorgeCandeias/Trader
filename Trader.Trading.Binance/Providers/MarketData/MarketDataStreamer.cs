using AutoMapper;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal partial class MarketDataStreamer : IMarketDataStreamer
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IMarketDataStreamClientFactory _streams;
    private readonly ITickerProvider _tickers;
    private readonly IKlineProvider _klines;

    public MarketDataStreamer(ILogger<MarketDataStreamer> logger, IMapper mapper, IMarketDataStreamClientFactory factory, ITickerProvider tickers, IKlineProvider klines)
    {
        _logger = logger;
        _mapper = mapper;
        _streams = factory;
        _tickers = tickers;
        _klines = klines;
    }

    private static string TypeName => nameof(MarketDataStreamer);

    public Task StreamAsync(IEnumerable<string> tickers, IEnumerable<(string Symbol, KlineInterval Interval)> klines, CancellationToken cancellationToken = default)
    {
        return StreamCoreAsync(tickers, klines, cancellationToken);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "N/A")]
    private async Task StreamCoreAsync(IEnumerable<string> tickers, IEnumerable<(string Symbol, KlineInterval Interval)> klines, CancellationToken cancellationToken)
    {
        var tickerLookup = tickers.ToHashSet();
        var klineLookup = klines.ToHashSet();

        // create a client for the streams we want
        var streams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        streams.UnionWith(tickerLookup.Select(x => $"{x.ToLowerInvariant()}@miniTicker"));
        streams.UnionWith(klineLookup.Select(x => $"{x.Symbol.ToLowerInvariant()}@kline_{_mapper.Map<string>(x.Interval)}"));

        LogConnectingToStreams(TypeName, streams);

        using var client = _streams.Create(streams);

        await client
            .ConnectAsync(cancellationToken)
            .ConfigureAwait(false);

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
                await _tickers.ConflateTickerAsync(message.MiniTicker, cancellationToken).ConfigureAwait(false);
            }

            if (message.Kline is not null && klineLookup.Contains((message.Kline.Symbol, message.Kline.Interval)))
            {
                await _klines.ConflateKlineAsync(message.Kline, cancellationToken).ConfigureAwait(false);
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