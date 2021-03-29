using AutoMapper;
using System.Linq;

namespace Trader.Data.Converters
{
    internal class OrderGroupConverter : ITypeConverter<OrderGroupEntity, OrderGroup>
    {
        public OrderGroup Convert(OrderGroupEntity source, OrderGroup destination, ResolutionContext context)
        {
            var orders = context.Mapper.Map<SortedOrderSet>(source.Details.Select(x => x.Order));

            return new OrderGroup(source.Id, orders);
        }
    }
}