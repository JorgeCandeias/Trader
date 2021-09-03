using System;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models
{
    public record ExchangeInfo(
        TimeZoneInfo TimeZone,
        DateTime ServerTime,
        ImmutableList<RateLimit> RateLimits,
        ImmutableList<ExchangeFilter> ExchangeFilters,
        ImmutableList<Symbol> Symbols);

    public record RateLimit(
        RateLimitType Type,
        TimeSpan TimeSpan,
        int Limit);

    public record ExchangeFilter;

    public record ExchangeMaxNumberOfOrdersFilter(int Value) : ExchangeFilter;

    public record ExchangeMaxNumberOfAlgoOrdersFilter(int Value) : ExchangeFilter;

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
        ImmutableList<SymbolFilter> Filters,
        ImmutableList<Permission> Permissions);

    public record SymbolFilter;
    public record PriceSymbolFilter(decimal MinPrice, decimal MaxPrice, decimal TickSize) : SymbolFilter;
    public record PercentPriceSymbolFilter(decimal MultiplierUp, decimal MultiplierDown, int AvgPriceMins) : SymbolFilter;
    public record LotSizeSymbolFilter(decimal MinQuantity, decimal MaxQuantity, decimal StepSize) : SymbolFilter;
    public record MinNotionalSymbolFilter(decimal MinNotional, bool ApplyToMarket, int AvgPriceMins) : SymbolFilter;
    public record IcebergPartsSymbolFilter(int Limit) : SymbolFilter;
    public record MarketLotSizeSymbolFilter(decimal MinQuantity, decimal MaxQuantity, decimal StepSize) : SymbolFilter;
    public record MaxNumberOfOrdersSymbolFilter(int MaxNumberOfOrders) : SymbolFilter;
    public record MaxNumberOfAlgoOrdersSymbolFilter(int MaxNumberOfAlgoOrders) : SymbolFilter;
    public record MaxNumberOfIcebergOrdersSymbolFilter(int MaxNumberOfIcebergOrders) : SymbolFilter;
    public record MaxPositionSymbolFilter(decimal MaxPosition) : SymbolFilter;
}