using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.CancelOrder
{
    internal class CancelOrderExecutor : IAlgoCommandExecutor<CancelOrderCommand>
    {
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IOrderProvider _orders;

        public CancelOrderExecutor(ILogger<CancelOrderExecutor> logger, ITradingService trader, IOrderProvider orders)
        {
            _logger = logger;
            _trader = trader;
            _orders = orders;
        }

        private static string TypeName => nameof(CancelOrderExecutor);

        public async ValueTask ExecuteAsync(IAlgoContext context, CancelOrderCommand command, CancellationToken cancellationToken = default)
        {
            LogStart(_logger, command.Symbol.Name, command.OrderId);

            var watch = Stopwatch.StartNew();

            var order = await _trader
                .CancelOrderAsync(command.Symbol.Name, command.OrderId, cancellationToken)
                .ConfigureAwait(false);

            await _orders
                .SetOrderAsync(order, cancellationToken)
                .ConfigureAwait(false);

            LogEnd(_logger, command.Symbol.Name, command.OrderId, watch.ElapsedMilliseconds);
        }

        private static readonly Action<ILogger, string, string, long, Exception> _logStart = LoggerMessage.Define<string, string, long>(
            LogLevel.Information, new EventId(0, nameof(LogStart)),
            "{Type} {Symbol} cancelling order {OrderId}...");

        private static void LogStart(ILogger logger, string symbol, long orderId) =>
            _logStart(logger, TypeName, symbol, orderId, null!);

        private static readonly Action<ILogger, string, string, long, long, Exception> _logEnd = LoggerMessage.Define<string, string, long, long>(
            LogLevel.Information, new EventId(0, nameof(LogEnd)),
            "{Type} {Symbol} cancelled order {OrderId} in {ElapsedMs}ms");

        private static void LogEnd(ILogger logger, string symbol, long orderId, long elapsedMs) =>
            _logEnd(logger, TypeName, symbol, orderId, elapsedMs, null!);
    }
}