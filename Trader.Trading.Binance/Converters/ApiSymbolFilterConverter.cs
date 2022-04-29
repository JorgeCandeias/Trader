namespace Outcompute.Trader.Trading.Binance.Converters;

internal partial class ApiSymbolFilterConverter : ITypeConverter<ApiSymbolFilter, SymbolFilter>
{
    private readonly ILogger<ApiSymbolFilterConverter> _logger;

    public ApiSymbolFilterConverter(ILogger<ApiSymbolFilterConverter> logger)
    {
        _logger = logger;
    }

    public SymbolFilter Convert(ApiSymbolFilter source, SymbolFilter destination, ResolutionContext context)
    {
        SymbolFilter result = source.FilterType switch
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
            "TRAILING_DELTA" => new TrailingDeltaSymbolFilter(source.MinTrailingAboveDelta, source.MaxTrailingAboveDelta, source.MinTrailingBelowDelta, source.MaxTrailingBelowDelta),

            _ => UnknownSymbolFilter.Empty
        };

        if (result is UnknownSymbolFilter)
        {
            LogUnknownSymbolFilter(source);
        }

        return result;
    }

    [LoggerMessage(1, LogLevel.Warning, "Unknown filter {Source}")]
    private partial void LogUnknownSymbolFilter(ApiSymbolFilter source);
}