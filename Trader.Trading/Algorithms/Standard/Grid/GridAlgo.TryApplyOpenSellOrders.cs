using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        protected IAlgoCommand? TryApplyOpenSellOrders()
        {
            // keeps track of used bands so we dont apply duplicate sell orders
            HashSet<Band>? used = null;

            // apply open sell orders to the bands
            foreach (var order in Context.Orders.Where(x => x.Side == OrderSide.Sell && x.Status.IsTransientStatus()))
            {
                // lazy create the used hashset to minimize garbage
                used ??= new HashSet<Band>(_bands.Count, BandEqualityComparer.Default);

                // attempt to find the band that matches the sell order
                var band = _bands.Except(used).SingleOrDefault(x => x.ClosePrice == order.Price && x.Quantity == order.OriginalQuantity);

                // if we found the band then track the active sell order on it
                if (band is not null)
                {
                    band.CloseOrderId = order.OrderId;
                    used.Add(band);
                }
            }

            _logger.LogInformation(
                "{Type} {Name} is managing {Count} bands",
                TypeName, Context.Name, _bands.Count, _bands);

            // always let the algo continue
            return null;
        }
    }
}