using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using System.Buffers;

namespace Outcompute.Trader.Trading.Commands.TrackingBuy;

internal class TrackingBuyExecutor : IAlgoCommandExecutor<TrackingBuyCommand>
{
    private readonly ILogger _logger;

    public TrackingBuyExecutor(ILogger<TrackingBuyExecutor> logger)
    {
        _logger = logger;
    }

    private static string TypeName => nameof(TrackingBuyExecutor);

    public async ValueTask ExecuteAsync(IAlgoContext context, TrackingBuyCommand command, CancellationToken cancellationToken = default)
    {
        var data = context.Data[command.Symbol.Name];
        var ticker = data.Ticker;
        var orders = data.Orders.Open.Where(x => x.Side == OrderSide.Buy).ToImmutableSortedSet(OrderQueryResult.KeyComparer);
        var balance = data.Spot.QuoteAsset;

        // identify the free balance
        var free = balance.Free;

        // identify the target low price for the first buy
        var lowBuyPrice = command.Symbol.LowerPriceToTickSize(ticker.ClosePrice * command.PullbackRatio);

        _logger.LogInformation(
            "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
            TypeName, command.Symbol.Name, lowBuyPrice, command.Symbol.QuoteAsset, ticker.ClosePrice, command.Symbol.QuoteAsset);

        orders = await TryCloseLowBuysAsync(context, command.Symbol, orders, lowBuyPrice, cancellationToken).ConfigureAwait(false);

        orders = await TryCloseHighBuysAsync(context, command.Symbol, orders, cancellationToken).ConfigureAwait(false);

        // if there are still open orders then leave them be
        if (orders.Count > 0) return;

        // calculate the target notional
        var total = free * command.TargetQuoteBalanceFractionPerBuy;

        // cap it at the max notional
        if (command.MaxNotional.HasValue)
        {
            total = Math.Min(total, command.MaxNotional.Value);
        }

        // bump it to the minimum notional if needed
        total = command.Symbol.RaiseTotalUpToMinNotional(total);

        // calculate the appropriate quantity to buy
        var quantity = total / lowBuyPrice;

        // round it up to the lot size step
        quantity = command.Symbol.RaiseQuantityToLotStepSize(quantity);

        // calculate the true notional after adjustments
        total = quantity * lowBuyPrice;

        // check if it still is under the max notional after adjustments - some assets have very high minimum notionals or lot sizes
        if (command.MaxNotional.HasValue && total > command.MaxNotional)
        {
            _logger.LogError(
                "{Type} {Name} cannot place buy order with amount of {Total} {Quote} because it is above the configured maximum notional of {MaxNotional}",
                TypeName, command.Symbol.Name, total, command.Symbol.QuoteAsset, command.MaxNotional);

            return;
        }

        _logger.LogInformation(
            "{Type} {Name} placing {OrderType} {OrderSode} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
            TypeName, command.Symbol.Name, OrderType.Limit, OrderSide.Buy, command.Symbol.Name, quantity, command.Symbol.BaseAsset, lowBuyPrice, command.Symbol.QuoteAsset, quantity * lowBuyPrice, command.Symbol.QuoteAsset);

        // place the order now
        var tag = $"{command.Symbol.Name}{lowBuyPrice:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);

        await new CreateOrderCommand(command.Symbol, OrderType.Limit, OrderSide.Buy, TimeInForce.GoodTillCanceled, quantity, null, lowBuyPrice, null, tag)
            .ExecuteAsync(context, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ImmutableSortedSet<OrderQueryResult>> TryCloseLowBuysAsync(IAlgoContext context, Symbol symbol, ImmutableSortedSet<OrderQueryResult> orders, decimal lowBuyPrice, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<OrderQueryResult>.Shared.Rent(orders.Count);
        var count = 0;

        // cancel all open buy orders with an open price lower than the lower band to the current price
        foreach (var order in orders.Where(x => x.Side == OrderSide.Buy && x.Price < lowBuyPrice))
        {
            _logger.LogInformation(
                "{Type} {Name} cancelling low starting open order with price {Price} for {Quantity} units",
                TypeName, symbol.Name, order.Price, order.OriginalQuantity);

            await new CancelOrderCommand(symbol, order.OrderId)
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);

            buffer[count++] = order;
        }

        if (count > 0)
        {
            orders = orders.Except(buffer.Take(count));
        }

        ArrayPool<OrderQueryResult>.Shared.Return(buffer);

        return orders;
    }

    private async Task<ImmutableSortedSet<OrderQueryResult>> TryCloseHighBuysAsync(IAlgoContext context, Symbol symbol, ImmutableSortedSet<OrderQueryResult> orders, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<OrderQueryResult>.Shared.Rent(orders.Count);
        var count = 0;

        foreach (var order in orders.Where(x => x.Side == OrderSide.Buy).OrderBy(x => x.Price).Skip(1))
        {
            _logger.LogInformation(
                "{Type} {Name} cancelling low starting open order with price {Price} for {Quantity} units",
                TypeName, symbol.Name, order.Price, order.OriginalQuantity);

            await new CancelOrderCommand(symbol, order.OrderId)
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);

            buffer[count++] = order;
        }

        if (count > 0)
        {
            orders = orders.Except(buffer.Take(count));
        }

        ArrayPool<OrderQueryResult>.Shared.Return(buffer);

        return orders;
    }
}