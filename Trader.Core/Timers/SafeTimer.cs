using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Core.Timers
{
    internal sealed class SafeTimer : ISafeTimer
    {
        private readonly Func<CancellationToken, Task> _callback;
        private readonly TimeSpan _period;
        private readonly TimeSpan _timeout;
        private readonly ILogger _logger;

        public SafeTimer(Func<CancellationToken, Task> callback, TimeSpan dueTime, TimeSpan period, TimeSpan timeout, ILogger<SafeTimer> logger)
        {
            _callback = callback;
            _period = period;
            _timeout = timeout;
            _logger = logger;

            // schedule the first tick only
            _timer = new Timer(Handler, null, dueTime, Timeout.InfiniteTimeSpan);
        }

        private static string Name => nameof(SafeTimer);
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Timer _timer;

        [SuppressMessage("Major Bug", "S3168:\"async\" methods should not return \"void\"", Justification = "Timer Event Handler")]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Timer Event Handler")]
        private async void Handler(object? _)
        {
            // execute the current tick
            try
            {
                using var timeoutCancellation = new CancellationTokenSource(_timeout);
                using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token, timeoutCancellation.Token);

                await _callback(combinedCancellation.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} caught exception {Message}", Name, ex.Message);
            }

            // schedule the next tick
            try
            {
                _timer.Change(_period, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // noop
            }
        }

        #region Disposable

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _timer.Dispose();

                _cancellation.Cancel();
                _cancellation.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SafeTimer()
        {
            Dispose(false);
        }

        #endregion Disposable
    }
}