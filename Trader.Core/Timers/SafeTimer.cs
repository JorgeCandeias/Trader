using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Core.Timers
{
    internal sealed class SafeTimer : ISafeTimer, IDisposable
    {
        private readonly Func<IDisposable, Task> _callback;
        private readonly TimeSpan _dueTime;
        private readonly TimeSpan _period;
        private readonly ILogger _logger;

        public SafeTimer(Func<IDisposable, Task> callback, TimeSpan dueTime, TimeSpan period, ILogger<SafeTimer> logger)
        {
            _callback = callback;
            _dueTime = dueTime;
            _period = period;
            _logger = logger;
        }

        private Timer? _timer;

        private async void Handler(object? _)
        {
            // execute the current tick
            try
            {
                await _callback(this);
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

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}