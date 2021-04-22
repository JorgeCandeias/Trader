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
        private readonly TimeSpan _dueTime;
        private readonly TimeSpan _period;
        private readonly TimeSpan _timeout;
        private readonly ILogger _logger;

        public SafeTimer(Func<CancellationToken, Task> callback, TimeSpan dueTime, TimeSpan period, TimeSpan timeout, ILogger<SafeTimer> logger)
        {
            _callback = callback;
            _dueTime = dueTime;
            _period = period;
            _timeout = timeout;
            _logger = logger;
        }

        private static string Name => nameof(SafeTimer);
        private CancellationTokenSource? _cancellation;
        private Timer? _timer;
        private Task _task = Task.CompletedTask;
        private readonly object _lock = new();

        [SuppressMessage("Major Bug", "S3168:\"async\" methods should not return \"void\"", Justification = "Timer Event Handler")]
        private async void Handler(object? _)
        {
            // execute the current tick
            try
            {
                using var timeoutCancellation = new CancellationTokenSource(_timeout);
                using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellation?.Token ?? default, timeoutCancellation.Token);

                _task = _callback(combinedCancellation.Token);

                await _task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} caught exception {Message}", Name, ex.Message);
            }

            // schedule the next tick
            lock (_lock)
            {
                try
                {
                    _timer?.Change(_period, Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException)
                {
                    // noop
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            // quick path for an already running timer
            if (_timer is not null) return Task.CompletedTask;

            // thread-safe path for starting the timer
            lock (_lock)
            {
                // thread-safe path for an already running timer
                if (_timer is not null) return Task.CompletedTask;

                // renew the cancellation
                _cancellation = new();

                // renew the timer
                _timer = new Timer(Handler, null, _dueTime, Timeout.InfiniteTimeSpan);
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            _logger.LogInformation("{ServiceName} stopping...", Name);

            // quick path for an already stopped timer
            if (_timer is null) return;

            // thread-safe path for stopping the timer
            Task task;
            lock (_lock)
            {
                // thread-safe check for the stopped timer
                if (_timer is null) return;

                // release the cancellation - this will also raise the token
                _cancellation?.Dispose();
                _cancellation = null;

                // release the dotnet timer to stop ticking
                _timer.Dispose();
                _timer = null;

                // keep the last task so we can await for it outside the lock
                task = _task;

                // release the last task for garbage collection after we are done with it
                _task = Task.CompletedTask;
            }

            // await for the last task now
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} caught exception {Message}", Name, ex.Message);
            }
        }

        #region Disposable

        private bool _disposed = false;

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellation?.Dispose();
                _timer?.Dispose();
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

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(Name);
            }
        }

        #endregion Disposable
    }
}