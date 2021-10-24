using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks
{
    internal class CreateOrderBlock : ICreateOrderBlock
    {
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IOrderProvider _orders;

        public CreateOrderBlock(ILogger<CreateOrderBlock> logger, ITradingService trader, IOrderProvider orders)
        {
            _logger = logger;
            _trader = trader;
            _orders = orders;
        }

        private static string TypeName => nameof(CreateOrderBlock);

        public Task<OrderResult> CreateOrderAsync(Symbol symbol, OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return CreateOrderCoreAsync(symbol, type, side, timeInForce, quantity, price, tag, cancellationToken);
        }

        private async Task<OrderResult> CreateOrderCoreAsync(Symbol symbol, OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag, CancellationToken cancellationToken = default)
        {
            // if we got here then we can place the order
            var watch = Stopwatch.StartNew();

            _logger.LogInformation(
                "{Type} {Name} placing {OrderType} {OrderSide} order for {Quantity:F8} {Asset} at {Price:F8} {Quote} for a total of {Total:F8} {Quote}",
                TypeName, symbol.Name, type, side, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset);

            var result = await _trader
                .CreateOrderAsync(symbol.Name, side, type, timeInForce, quantity, null, price, tag, null, null, cancellationToken)
                .ConfigureAwait(false);

            await _orders
                .SetOrderAsync(result, 0m, 0m, 0m, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "{Type} {Name} placed {OrderType} {OrderSide} order for {Quantity:F8} {Asset} at {Price:F8} {Quote} for a total of {Total:F8} {Quote} in {ElapsedMs}ms",
                TypeName, symbol.Name, type, side, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, watch.ElapsedMilliseconds);

            return result;
        }
    }
}