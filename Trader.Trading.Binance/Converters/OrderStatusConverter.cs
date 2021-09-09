using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class OrderStatusConverter : ITypeConverter<string, OrderStatus>
    {
        public OrderStatus Convert(string source, OrderStatus destination, ResolutionContext context)
        {
            return source switch
            {
                null => OrderStatus.None,

                "NEW" => OrderStatus.New,
                "PARTIALLY_FILLED" => OrderStatus.PartiallyFilled,
                "FILLED" => OrderStatus.Filled,
                "CANCELED" => OrderStatus.Canceled,
                "PENDING_CANCEL" => OrderStatus.PendingCancel,
                "REJECTED" => OrderStatus.Rejected,
                "EXPIRED" => OrderStatus.Expired,

                _ => throw new AutoMapperMappingException($"Unknown {nameof(OrderStatus)} '{source}'")
            };
        }
    }
}