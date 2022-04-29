using Orleans.Concurrency;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Models;

[Immutable]
public record ExchangeInfo(
    TimeZoneInfo TimeZone,
    DateTime ServerTime,
    ImmutableList<RateLimit> RateLimits,
    ImmutableList<ExchangeFilter> ExchangeFilters,
    ImmutableList<Symbol> Symbols)
{
    public static ExchangeInfo Empty { get; } = new ExchangeInfo(
        TimeZoneInfo.Utc,
        DateTime.MinValue,
        ImmutableList<RateLimit>.Empty,
        ImmutableList<ExchangeFilter>.Empty,
        ImmutableList<Symbol>.Empty);
}

[Immutable]
public record RateLimit(
    RateLimitType Type,
    TimeSpan TimeSpan,
    int Limit);

[Immutable]
public record ExchangeFilter;

[Immutable]
public record ExchangeMaxNumberOfOrdersFilter(int Value) : ExchangeFilter;

[Immutable]
public record ExchangeMaxNumberOfAlgoOrdersFilter(int Value) : ExchangeFilter;

[Immutable]
public record Symbol(
    string Name,
    SymbolStatus Status,
    string BaseAsset,
    int BaseAssetPrecision,
    string QuoteAsset,
    int QuoteAssetPrecision,
    int BaseCommissionPrecision,
    int QuoteCommissionPrecision,
    ImmutableList<OrderType> OrderTypes,
    bool IsIcebergAllowed,
    bool IsOcoAllowed,
    bool IsQuoteOrderQuantityMarketAllowed,
    bool IsSpotTradingAllowed,
    bool IsMarginTradingAllowed,
    SymbolFilters Filters,
    ImmutableList<Permission> Permissions)
{
    public static Symbol Empty { get; } = new Symbol(
        string.Empty,
        SymbolStatus.None,
        string.Empty,
        0,
        string.Empty,
        0,
        0,
        0,
        ImmutableList<OrderType>.Empty,
        false,
        false,
        false,
        false,
        false,
        SymbolFilters.Empty,
        ImmutableList<Permission>.Empty);

    public static IComparer<Symbol> NameComparer { get; } = new NameComparerInternal();

    private sealed class NameComparerInternal : IComparer<Symbol>, IEqualityComparer<Symbol>
    {
        public int Compare(Symbol? x, Symbol? y)
        {
            if (x is null) return y is null ? 0 : -1;
            if (y is null) return 1;

            return StringComparer.Ordinal.Compare(x.Name, y.Name);
        }

        public bool Equals(Symbol? x, Symbol? y)
        {
            if (x is null) return y is null;
            if (y is null) return false;

            return StringComparer.Ordinal.Equals(x.Name, y.Name);
        }

        public int GetHashCode([DisallowNull] Symbol obj)
        {
            return obj.Name.GetHashCode(StringComparison.Ordinal);
        }
    }
}

[Immutable]
public record SymbolFilters(
    PriceSymbolFilter Price,
    PercentPriceSymbolFilter PercentPrice,
    LotSizeSymbolFilter LotSize,
    MinNotionalSymbolFilter MinNotional,
    IcebergPartsSymbolFilter IcebergParts,
    MarketLotSizeSymbolFilter MarketLotSize,
    MaxNumberOfOrdersSymbolFilter MaxNumberOfOrders,
    MaxNumberOfAlgoOrdersSymbolFilter MaxNumberOfAlgoOrders,
    MaxNumberOfIcebergOrdersSymbolFilter MaxNumberOfIcebergOrders,
    MaxPositionSymbolFilter MaxPositions)
{
    public static SymbolFilters Empty { get; } = new SymbolFilters(
        PriceSymbolFilter.Empty,
        PercentPriceSymbolFilter.Empty,
        LotSizeSymbolFilter.Empty,
        MinNotionalSymbolFilter.Empty,
        IcebergPartsSymbolFilter.Empty,
        MarketLotSizeSymbolFilter.Empty,
        MaxNumberOfOrdersSymbolFilter.Empty,
        MaxNumberOfAlgoOrdersSymbolFilter.Empty,
        MaxNumberOfIcebergOrdersSymbolFilter.Empty,
        MaxPositionSymbolFilter.Empty);
}

[Immutable]
public record SymbolFilter;

[Immutable]
public record UnknownSymbolFilter : SymbolFilter
{
    public static UnknownSymbolFilter Empty { get; } = new UnknownSymbolFilter();
}

[Immutable]
public record PriceSymbolFilter(decimal MinPrice, decimal MaxPrice, decimal TickSize) : SymbolFilter
{
    public static PriceSymbolFilter Empty { get; } = new PriceSymbolFilter(0m, 0m, 0m);
}

[Immutable]
public record PercentPriceSymbolFilter(decimal MultiplierUp, decimal MultiplierDown, int AvgPriceMins) : SymbolFilter
{
    public static PercentPriceSymbolFilter Empty { get; } = new PercentPriceSymbolFilter(0m, 0m, 0);
}

[Immutable]
public record LotSizeSymbolFilter(decimal MinQuantity, decimal MaxQuantity, decimal StepSize) : SymbolFilter
{
    public static LotSizeSymbolFilter Empty { get; } = new LotSizeSymbolFilter(0m, 0m, 0m);
}

[Immutable]
public record MinNotionalSymbolFilter(decimal MinNotional, bool ApplyToMarket, int AvgPriceMins) : SymbolFilter
{
    public static MinNotionalSymbolFilter Empty { get; } = new MinNotionalSymbolFilter(0m, false, 0);
}

[Immutable]
public record IcebergPartsSymbolFilter(int Limit) : SymbolFilter
{
    public static IcebergPartsSymbolFilter Empty { get; } = new IcebergPartsSymbolFilter(0);
}

[Immutable]
public record MarketLotSizeSymbolFilter(decimal MinQuantity, decimal MaxQuantity, decimal StepSize) : SymbolFilter
{
    public static MarketLotSizeSymbolFilter Empty { get; } = new MarketLotSizeSymbolFilter(0m, 0m, 0m);
}

[Immutable]
public record MaxNumberOfOrdersSymbolFilter(int MaxNumberOfOrders) : SymbolFilter
{
    public static MaxNumberOfOrdersSymbolFilter Empty { get; } = new MaxNumberOfOrdersSymbolFilter(0);
}

[Immutable]
public record MaxNumberOfAlgoOrdersSymbolFilter(int MaxNumberOfAlgoOrders) : SymbolFilter
{
    public static MaxNumberOfAlgoOrdersSymbolFilter Empty { get; } = new MaxNumberOfAlgoOrdersSymbolFilter(0);
}

[Immutable]
public record MaxNumberOfIcebergOrdersSymbolFilter(int MaxNumberOfIcebergOrders) : SymbolFilter
{
    public static MaxNumberOfIcebergOrdersSymbolFilter Empty { get; } = new MaxNumberOfIcebergOrdersSymbolFilter(0);
}

[Immutable]
public record MaxPositionSymbolFilter(decimal MaxPosition) : SymbolFilter
{
    public static MaxPositionSymbolFilter Empty { get; } = new MaxPositionSymbolFilter(0m);
}

[Immutable]
public record TrailingDeltaSymbolFilter(int MinTrailingAboveDelta, int MaxTrailingAboveDelta, int MinTrailingBelowDelta, int MaxTrailingBelowDelta) : SymbolFilter
{
    public static TrailingDeltaSymbolFilter Empty { get; } = new TrailingDeltaSymbolFilter(0, 0, 0, 0);
}