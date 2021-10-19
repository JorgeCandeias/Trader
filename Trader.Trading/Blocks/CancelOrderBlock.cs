using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class CancelOrderBlock
    {
        public static ValueTask<CancelStandardOrderResult> CancelOrderAsync(this IAlgoContext context, string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(context));

            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();
            var trader = context.ServiceProvider.GetRequiredService<ITradingService>();
            var orderProvider = context.ServiceProvider.GetRequiredService<IOrderProvider>();

            return CancelOrderInnerAsync(symbol, orderId, logger, trader, orderProvider, cancellationToken);
        }

        private static async ValueTask<CancelStandardOrderResult> CancelOrderInnerAsync(string symbol, long orderId, ILogger logger, ITradingService trader, IOrderProvider orderProvider, CancellationToken cancellationToken = default)
        {
            logger.LogStart(symbol, orderId);

            var watch = Stopwatch.StartNew();

            var order = await trader
                .CancelOrderAsync(symbol, orderId, cancellationToken)
                .ConfigureAwait(false);

            await orderProvider
                .SetOrderAsync(order, cancellationToken)
                .ConfigureAwait(false);

            logger.LogEnd(symbol, orderId, watch.ElapsedMilliseconds);

            return order;
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