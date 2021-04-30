using AutoMapper;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading.Binance
{
    internal sealed class BinanceUserDataStreamWssClient : IUserDataStreamClient
    {
        private readonly string _listenKey;
        private readonly BinanceOptions _options;
        private readonly IMapper _mapper;

        public BinanceUserDataStreamWssClient(string listenKey, IOptions<BinanceOptions> options, IMapper mapper)
        {
            _listenKey = listenKey ?? throw new ArgumentNullException(nameof(listenKey));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        private readonly ClientWebSocket _client = new();

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            await _client.ConnectAsync(new Uri(_options.BaseWssAddress, $"/ws/{_listenKey}"), cancellationToken).ConfigureAwait(false);
        }

        public async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<UserDataStreamMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            using var buffer = MemoryPool<byte>.Shared.Rent(1 << 10);

            var total = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _client
                    .ReceiveAsync(buffer.Memory[total..], cancellationToken)
                    .ConfigureAwait(false);

                total += result.Count;

                // break if we got the entire message
                if (result.EndOfMessage) break;

                // throw if we ran out of buffer
                if (total >= buffer.Memory.Length) throw new InvalidOperationException($"Could not load web socket message into a buffer of length '{buffer.Memory.Length}'.");
            }

            return _mapper.Map<UserDataStreamMessage>(buffer.Memory.Slice(0, total));
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

        ~BinanceUserDataStreamWssClient()
        {
            Dispose(false);
        }

        #endregion Disposable
    }
}