using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class SymbolFilterConverter : ITypeConverter<SymbolFilterModel, SymbolFilter>
    {
        public SymbolFilter Convert(SymbolFilterModel source, SymbolFilter destination, ResolutionContext context)
        {
            return source.FilterType switch
            {
                null => null!,

                "PRICE_FILTER" => new PriceSymbolFilter(source.MinPrice, source.MaxPrice, source.TickSize),
                "PERCENT_PRICE" => new PercentPriceSymbolFilter(source.MultiplierUp, source.MultiplierDown, source.AvgPriceMins),
                "LOT_SIZE" => new LotSizeSymbolFilter(source.MinQty, source.MaxQty, source.StepSize),
                "MIN_NOTIONAL" => new MinNotionalSymbolFilter(source.MinNotional, source.ApplyToMarket, source.AvgPriceMins),
                "ICEBERG_PARTS" => new IcebergPartsSymbolFilter(source.Limit),
                "MARKET_LOT_SIZE" => new MarketLotSizeSymbolFilter(source.MinQty, source.MaxQty, source.StepSize),
                "MAX_NUM_ORDERS" => new MaxNumberOfOrdersSymbolFilter(source.MaxNumOrders),
                "MAX_NUM_ALGO_ORDERS" => new MaxNumberOfAlgoOrdersSymbolFilter(source.MaxNumAlgoOrders),
                "MAX_NUM_ICEBERG_ORDERS" => new MaxNumberOfIcebergOrdersSymbolFilter(source.MaxNumIcebergOrders),
                "MAX_POSITION" => new MaxPositionSymbolFilter(source.MaxPosition),

                _ => throw new AutoMapperMappingException($"Unknown {nameof(source.FilterType)} '{source.FilterType}'")
            };
        }
    }
}