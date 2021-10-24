using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations
{
    internal class GetOpenOrdersOperation : IGetOpenOrdersOperation
    {
        private readonly ILogger _logger;
        private readonly IOrderProvider _orders;

        public GetOpenOrdersOperation(ILogger<GetOpenOrdersOperation> logger, IOrderProvider orders)
        {
            _logger = logger;
            _orders = orders;
        }

        private static string TypeName => nameof(GetOpenOrdersOperation);

        public Task<IReadOnlyList<OrderQueryResult>> GetOpenOrdersAsync(Symbol symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return GetOpenOrdersCoreAsync(symbol, side, cancellationToken);
        }

        private async Task<IReadOnlyList<OrderQueryResult>> GetOpenOrdersCoreAsync(Symbol symbol, OrderSide side, CancellationToken cancellationToken)
        {
            var orders = await _orders
                .GetTransientOrdersBySideAsync(symbol.Name, side, cancellationToken)
                .ConfigureAwait(false);

            foreach (var order in orders)
            {
                _logger.LogInformation(
                    "{Type} {Name} identified open {OrderSide} {OrderType} order for {Quantity} {Asset} at {Price} {Quote} totalling {Notional:N8} {Quote}",
                    TypeName, symbol.Name, order.Side, order.Type, order.OriginalQuantity, symbol.BaseAsset, order.Price, symbol.QuoteAsset, order.OriginalQuantity * order.Price, symbol.QuoteAsset);
            }

            return orders;
        }
    }
}