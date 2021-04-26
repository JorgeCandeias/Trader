using AutoMapper;
using Trader.Models;

namespace Trader.Trading.Binance.Converters
{
    internal class CancelAllOrdersResponseModelConverter : ITypeConverter<CancelAllOrdersResponseModel, CancelOrderResult>
    {
        public CancelOrderResult Convert(CancelAllOrdersResponseModel source, CancelOrderResult destination, ResolutionContext context)
        {
            if (source is null) return null!;

            return source.OrderListId switch
            {
                -1 => context.Mapper.Map<CancelStandardOrderResult>(source),

                _ => context.Mapper.Map<CancelOcoOrderResult>(source)
            };
        }
    }
}