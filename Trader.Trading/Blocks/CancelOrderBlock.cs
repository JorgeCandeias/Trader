using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class CancelOrderBlock
    {
        public static ValueTask CancelOrderAsync(this IAlgoContext context, string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(context));

            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();
            var trader = context.ServiceProvider.GetRequiredService<ITradingService>();
            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();
            var clock = context.ServiceProvider.GetRequiredService<ISystemClock>();

            return CancelOrderInnerAsync(symbol, orderId, logger, trader, repository, clock, cancellationToken);
        }

        private static async ValueTask CancelOrderInnerAsync(string symbol, long orderId, ILogger logger, ITradingService trader, ITradingRepository repository, ISystemClock clock, CancellationToken cancellationToken = default)
        {
            logger.LogStart(symbol, orderId);

            var watch = Stopwatch.StartNew();

            var order = await trader
                .CancelOrderAsync(symbol, orderId, cancellationToken)
                .ConfigureAwait(false);

            await repository
                .SetOrderAsync(order, cancellationToken)
                .ConfigureAwait(false);

            logger.LogEnd(symbol, orderId, watch.ElapsedMilliseconds);
        }

        private static readonly Action<ILogger, string, string, long, Exception> _logStart = LoggerMessage.Define<string, string, long>(
            LogLevel.Information, new EventId(0, nameof(LogStart)),
            "{Type} {Symbol} cancelling order {OrderId}...");

        private static void LogStart(this ILogger logger, string symbol, long orderId) =>
            _logStart(logger, nameof(CancelOrderBlock), symbol, orderId, null!);

        private static readonly Action<ILogger, string, string, long, long, Exception> _logEnd = LoggerMessage.Define<string, string, long, long>(
            LogLevel.Information, new EventId(0, nameof(LogEnd)),
            "{Type} {Symbol} cancelled order {OrderId} in {ElapsedMs}ms");

        private static void LogEnd(this ILogger logger, string symbol, long orderId, long elapsedMs) =>
            _logEnd(logger, nameof(CancelOrderBlock), symbol, orderId, elapsedMs, null!);
    }
}