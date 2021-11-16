using AutoMapper;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using System.Buffers;
using System.Net.WebSockets;
using static System.String;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal sealed class BinanceMarketDataStreamWssClient : IMarketDataStreamClient
{
    private readonly IReadOnlyCollection<string> _streams;
    private readonly BinanceOptions _options;
    private readonly IMapper _mapper;

    public BinanceMarketDataStreamWssClient(IReadOnlyCollection<string> streams, IOptions<BinanceOptions> options, IMapper mapper)
    {
        _streams = streams ?? throw new ArgumentNullException(nameof(streams));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        if (streams.Count is 0) throw new ArgumentOutOfRangeException(nameof(streams));

        _client.Options.KeepAliveInterval = _options.MarketDataStreamKeepAliveInterval;
    }

    private readonly ClientWebSocket _client = new();

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return _client.ConnectAsync(new Uri(_options.BaseWssAddress, $"/stream?streams={Join('/', _streams)}"), cancellationToken);
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return _client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
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
                if (result.EndOfMessage) break;

                // throw if we ran out of buffer
                if (total >= buffer.Memory.Length) throw new InvalidOperationException($"Could not load web socket message into a buffer of length '{buffer.Memory.Length}'.");
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
                throw new InvalidOperationException($"Unknown {nameof(WebSocketMessageType)} '{result.MessageType}'");
            }
        }

        return _mapper.Map<MarketDataStreamMessage>(buffer.Memory.Slice(0, total));
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
}