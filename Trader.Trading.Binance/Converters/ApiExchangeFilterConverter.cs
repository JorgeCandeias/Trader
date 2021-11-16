using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters;

internal class ApiExchangeFilterConverter : ITypeConverter<ApiExchangeFilter, ExchangeFilter>
{
    public ExchangeFilter Convert(ApiExchangeFilter source, ExchangeFilter destination, ResolutionContext context)
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