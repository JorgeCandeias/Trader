using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System.Linq;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        protected IAlgoCommand? TryApplySignificantBuyOrders()
        {
            // apply the significant buy orders to the bands
            foreach (var order in Context.Significant.Orders.Where(x => x.Side == OrderSide.Buy))
            {
                if (order.Price is 0)
                {
                    _logger.LogError(
                        "{Type} {Name} identified a significant {OrderSide} {OrderType} order {OrderId} for {Quantity} {Asset} on {Time} with zero price and will let the algo refresh to pick up missing trades",
                        TypeName, Context.Name, order.Side, order.Type, order.OrderId, order.ExecutedQuantity, Context.Symbol.BaseAsset, order.Time);

                    return Noop();
                }

                if (order.Status.IsTransientStatus())
                {
                    // add transient orders with original quantity
                    _bands.Add(new Band
                    {
                        Quantity = order.OriginalQuantity,
                        OpenPrice = order.Price,
                        OpenOrderId = order.OrderId,
                        CloseOrderClientId = CreateTag(order.Symbol, order.Price),
                        Status = BandStatus.Ordered
                    });
                }
                else
                {
                    // add completed orders with executed quantity
                    _bands.Add(new Band
                    {
                        Quantity = order.ExecutedQuantity,
                        OpenPrice = order.Price,
                        OpenOrderId = order.OrderId,
                        CloseOrderClientId = CreateTag(order.Symbol, order.Price),
                        Status = BandStatus.Open
                    });
                }
            }

            // let the algo continue
            return null;
        }
    }
}