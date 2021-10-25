using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.CancelOrder
{
    public class CancelOrderService : ICancelOrderService
    {
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IOrderProvider _orders;

        public CancelOrderService(ILogger<CancelOrderService> logger, ITradingService trader, IOrderProvider orders)
        {
            _logger = logger;
            _trader = trader;
            _orders = orders;
        }

        public Task<CancelStandardOrderResult> CancelOrderAsync(Symbol symbol, long orderId, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return CancelOrderCoreAsync(symbol, orderId, cancellationToken);
        }

        private async Task<CancelStandardOrderResult> CancelOrderCoreAsync(Symbol symbol, long orderId, CancellationToken cancellationToken = default)
        {
            LogStart(_logger, symbol.Name, orderId);

            var watch = Stopwatch.StartNew();

            var order = await _trader
                .CancelOrderAsync(symbol.Name, orderId, cancellationToken)
                .ConfigureAwait(false);

            await _orders
                .SetOrderAsync(order, cancellationToken)
                .ConfigureAwait(false);

            LogEnd(_logger, symbol.Name, orderId, watch.ElapsedMilliseconds);

            return order;
        }

        private static readonly Action<ILogger, string, string, long, Exception> _logStart = LoggerMessage.Define<string, string, long>(
            LogLevel.Information, new EventId(0, nameof(LogStart)),
            "{Type} {Symbol} cancelling order {OrderId}...");

        private static void LogStart(ILogger logger, string symbol, long orderId) =>
            _logStart(logger, nameof(CancelOrderService), symbol, orderId, null!);

        private static readonly Action<ILogger, string, string, long, long, Exception> _logEnd = LoggerMessage.Define<string, string, long, long>(
            LogLevel.Information, new EventId(0, nameof(LogEnd)),
            "{Type} {Symbol} cancelled order {OrderId} in {ElapsedMs}ms");

        private static void LogEnd(ILogger logger, string symbol, long orderId, long elapsedMs) =>
            _logEnd(logger, nameof(CancelOrderService), symbol, orderId, elapsedMs, null!);
    }
}