using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System.Diagnostics;

namespace Outcompute.Trader.Trading.Algorithms;

internal partial class OrderSynchronizer : IOrderSynchronizer
{
    private readonly ILogger _logger;
    private readonly ITradingService _trader;
    private readonly IOrderProvider _orders;

    public OrderSynchronizer(ILogger<OrderSynchronizer> logger, ITradingService trader, IOrderProvider orders)
    {
        _logger = logger;
        _trader = trader;
        _orders = orders;
    }

    private static string TypeName => nameof(OrderSynchronizer);

    public Task SynchronizeOrdersAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return SynchronizeOrdersCoreAsync(symbol, cancellationToken);
    }

    private async Task SynchronizeOrdersCoreAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        var count = 0;

        // start from the order after the last one synced
        var orderId = (await _orders.GetLastSyncedOrderId(symbol, cancellationToken)) + 1;

        // if the last known transient order is earlier then start from that instead
        var transientOrderId = await _orders.TryGetMinTransientOrderIdAsync(symbol, cancellationToken);
        if (transientOrderId.HasValue && transientOrderId.Value < orderId)
        {
            orderId = transientOrderId.Value;
        }

        // pull all new or updated orders page by page
        while (!cancellationToken.IsCancellationRequested)
        {
            var orders = await _trader
                .WithBackoff()
                .GetAllOrdersAsync(symbol, orderId, 1000, cancellationToken)
                .ConfigureAwait(false);

            // break if we got no orders
            if (orders.Count is 0) break;

            // save the orders
            await _orders.SetOrdersAsync(symbol, orders, cancellationToken);

            // keep the last order id
            orderId = orders.Max(x => x.OrderId);

            // save the synced order id for the next time in case the next loop fails
            await _orders.SetLastSyncedOrderId(symbol, orderId, cancellationToken);

            // keep track for logging
            count += orders.Count;

            // break if we didnt get a full-ish page - using a leeway for when binance doesnt fill full pages by one or two items
            if (orders.Count < 990) break;

            // loop from the next
            orderId++;
        }

        // log the activity only if necessary
        LogPulledOrders(TypeName, symbol, count, orderId, watch.ElapsedMilliseconds);
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{TypeName} {Symbol} pulled {Count} orders up to OrderId {MaxOrderId} in {ElapsedMs}ms")]
    private partial void LogPulledOrders(string typeName, string symbol, int count, long maxOrderId, long elapsedMs);

    #endregion Logging
}