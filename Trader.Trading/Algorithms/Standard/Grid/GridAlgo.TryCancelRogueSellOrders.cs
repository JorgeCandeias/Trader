using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        /// <summary>
        /// Identify and cancel rogue sell orders that do not belong to a trading band.
        /// </summary>
        protected IAlgoCommand? TryCancelRogueSellOrders(IEnumerable<OrderQueryResult> transientSellOrders)
        {
            foreach (var orderId in transientSellOrders.Select(x => x.OrderId))
            {
                if (!_bands.Any(x => x.CloseOrderId == orderId))
                {
                    return CancelOrder(Context.Symbol, orderId);
                }
            }

            return null;
        }
    }
}