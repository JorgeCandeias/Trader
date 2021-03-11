using AutoMapper;

namespace Trader.Core.Trading.Binance.Converters
{
    internal class CancelAllOrdersResponseModelConverter : ITypeConverter<CancelAllOrdersResponseModel, CancelOrderResultBase>
    {
        public CancelOrderResultBase Convert(CancelAllOrdersResponseModel source, CancelOrderResultBase destination, ResolutionContext context)
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