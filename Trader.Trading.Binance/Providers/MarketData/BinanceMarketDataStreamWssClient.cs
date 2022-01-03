using Microsoft.Extensions.Options;
using Orleans.Runtime;
using System.Buffers;
using System.Net.WebSockets;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal sealed partial class BinanceMarketDataStreamWssClient : IMarketDataStreamClient
{
    private readonly ILogger _logger;
    private readonly IReadOnlyCollection<string> _streams;
    private readonly BinanceOptions _options;
    private readonly IMapper _mapper;

    public BinanceMarketDataStreamWssClient(ILogger<BinanceMarketDataStreamWssClient> logger, IReadOnlyCollection<string> streams, IOptions<BinanceOptions> options, IMapper mapper)
    {
        _logger = logger;
        _streams = streams;
        _options = options.Value;
        _mapper = mapper;

        _client.Options.KeepAliveInterval = _options.MarketDataStreamKeepAliveInterval;
    }

    private const string TypeName = nameof(BinanceMarketDataStreamWssClient);

    private readonly ClientWebSocket _client = new();

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return _client.ConnectAsync(new Uri(_options.BaseWssAddress, $"/stream{(_streams.Count > 0 ? $"?streams={Join('/', _streams)}" : "")}"), cancellationToken);
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return _client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
    }

    public async Task SubscribeAsync(long id, IEnumerable<string> streams, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(streams, nameof(streams));

        foreach (var batch in streams.BatchIEnumerable(10))
        {
            LogSubscribingToStreams(TypeName, batch);

            var model = new MarketDataStreamRequest("SUBSCRIBE", batch.ToArray(), id);
            var json = _mapper.Map<byte[]>(model);

            await _client.SendAsync(json, WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    public async Task<MarketDataStreamMessage> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent(1 << 20);
        var total = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await _client
                .ReceiveAsync(buffer.Memory[total..], cancellationToken)
                .ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                total += result.Count;

                // break if we got the entire message
                if (result.EndOfMessage)
                {
                    break;
                }

                // throw if we ran out of buffer
                if (total >= buffer.Memory.Length)
                {
                    throw new InvalidOperationException($"Could not load web socket message into a buffer of length '{buffer.Memory.Length}'.");
                }
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                // noop for now
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                // early break
                throw new InvalidOperationException("The server has closed the web socket");
            }
            else
            {
                LogUnknownMessageType(nameof(BinanceMarketDataStreamWssClient), result.MessageType);
            }
        }

        return _mapper.Map<MarketDataStreamMessage>(buffer.Memory[..total]);
    }

    #region Disposable

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _client.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BinanceMarketDataStreamWssClient()
    {
        Dispose(false);
    }

    #endregion Disposable

    #region Logging

    [LoggerMessage(1, LogLevel.Error, "{Type} received unknown message '{MessageType}'")]
    private partial void LogUnknownMessageType(string type, WebSocketMessageType messageType);

    [LoggerMessage(2, LogLevel.Information, "{Type} subscribing to streams {Streams}")]
    private partial void LogSubscribingToStreams(string type, IEnumerable<string> streams);

    #endregion Logging
}