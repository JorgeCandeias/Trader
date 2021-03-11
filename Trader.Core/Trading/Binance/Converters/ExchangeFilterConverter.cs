using AutoMapper;

namespace Trader.Core.Trading.Binance.Converters
{
    internal class ExchangeFilterConverter : ITypeConverter<ExchangeFilterModel, ExchangeFilter>
    {
        public ExchangeFilter Convert(ExchangeFilterModel source, ExchangeFilter destination, ResolutionContext context)
        {
            return source.FilterType switch
            {
                null => null!,

                "EXCHANGE_MAX_NUM_ORDERS" => new ExchangeMaxNumberOfOrdersFilter(source.MaxNumOrders ?? 0),
                "EXCHANGE_MAX_ALGO_ORDERS" => new ExchangeMaxNumberOfAlgoOrdersFilter(source.MaxNumAlgoOrders ?? 0),

                _ => throw new AutoMapperMappingException($"Unknown {nameof(source.FilterType)} '{source.FilterType}'")
            };
        }
    }
}