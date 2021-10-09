using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Binance;
using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading
{
    public static class BinanceTooManyRequestsExceptionTradingServiceExtensions
    {
        public static Task<T> WithWaitOnTooManyRequests<T>(this ITradingService trader, Func<ITradingService, CancellationToken, Task<T>> action, ILogger logger, CancellationToken cancellationToken = default)
        {
            if (trader is null) throw new ArgumentNullException(nameof(trader));
            if (action is null) throw new ArgumentNullException(nameof(action));

            return Policy
                 .Handle<BinanceTooManyRequestsException>()
                 .WaitAndRetryForeverAsync(
                     (n, ex, ctx) => ((BinanceTooManyRequestsException)ex).RetryAfter,
                     (ex, ts, ctx) => { logger.LogWarning(ex, "Backing off for {TimeSpan}...", ts); return Task.CompletedTask; })
                 .ExecuteAsync(ct => action(trader, ct), cancellationToken, true);
        }
    }
}