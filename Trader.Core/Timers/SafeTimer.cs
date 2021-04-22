using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
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

        private readonly CancellationTokenSource _cancellation = new();

        private Timer? _timer;
        private Task _task = Task.CompletedTask;

        [SuppressMessage("Major Bug", "S3168:\"async\" methods should not return \"void\"", Justification = "Timer Event Handler")]
        private async void Handler(object? _)
        {
            // execute the current tick
            try
            {
                using var timeoutCancellation = new CancellationTokenSource(_timeout);
                using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token, timeoutCancellation.Token);

                _task = _callback(combinedCancellation.Token);

                await _task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} caught exception {Message}", nameof(SafeTimer), ex.Message);

                if (Debugger.IsAttached)
                {
                    throw;
                }
            }

            // schedule the next tick
            try
            {
                _timer?.Change(_period, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // noop
            }
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_timer is null)
            {
                _timer = new Timer(Handler, null, _dueTime, Timeout.InfiniteTimeSpan);
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_timer is not null)
            {
                _cancellation.Cancel();
                _timer.Dispose();
                _timer = null;

                var task = Interlocked.Exchange(ref _task, Task.CompletedTask);
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                    // noop
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{ServiceName} caught exception {Message}", nameof(SafeTimer), ex.Message);
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}