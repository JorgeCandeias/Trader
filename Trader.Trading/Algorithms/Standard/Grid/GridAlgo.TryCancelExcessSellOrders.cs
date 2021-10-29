﻿using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        /// <summary>
        /// Identify and cancel excess sell orders above the limit.
        /// </summary>
        protected IAlgoCommand? TryCancelExcessSellOrders(IEnumerable<OrderQueryResult> transientSellOrders)
        {
            // get the order ids for the lowest open bands
            var bands = _bands
                .Where(x => x.Status == BandStatus.Open)
                .Take(_options.MaxActiveSellOrders)
                .Select(x => x.CloseOrderId)
                .Where(x => x is not 0)
                .ToHashSet();

            // cancel all excess sell orders now
            foreach (var orderId in transientSellOrders.Select(x => x.OrderId))
            {
                if (!bands.Contains(orderId))
                {
                    return CancelOrder(Context.Symbol, orderId);
                }
            }

            return null;
        }
    }
}