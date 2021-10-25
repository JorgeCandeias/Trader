using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.InMemory.UserData
{
    internal sealed class InMemoryUserDataStreamClient : IUserDataStreamClient
    {
        private readonly IDisposable _registration;

        public InMemoryUserDataStreamClient(IInMemoryUserDataStreamSender sender)
        {
            _registration = sender.Register((message, ct) => _channel.Writer.WriteAsync(message, ct).AsTask());
        }

        private readonly Channel<UserDataStreamMessage> _channel = Channel.CreateUnbounded<UserDataStreamMessage>();

        private CancellationTokenSource? _cancellation;

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Exchange(ref _cancellation, null)?.Dispose();

            return Task.CompletedTask;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Exchange(ref _cancellation, new())?.Dispose();

            return Task.CompletedTask;
        }

        public async Task<UserDataStreamMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            if (_cancellation is null) throw new InvalidOperationException();

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token, cancellationToken);

            return await _channel.Reader.ReadAsync(linked.Token).ConfigureAwait(false);
        }

        #region Disposable

        private bool _disposed;

        private void DisposeCore()
        {
            if (_disposed) return;

            _registration.Dispose();

            _disposed = true;
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        ~InMemoryUserDataStreamClient()
        {
            DisposeCore();
        }

        #endregion Disposable
    }
}