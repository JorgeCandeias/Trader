using Orleans.Concurrency;
using System;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models
{
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
        ImmutableList<SymbolFilter> Filters, // todo: refactor the filters into sub-properties so the algos don't have to iterate this collection
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
            ImmutableList<SymbolFilter>.Empty,
            ImmutableList<Permission>.Empty);
    }

    [Immutable]
    public record SymbolFilter;

    [Immutable]
    public record PriceSymbolFilter(decimal MinPrice, decimal MaxPrice, decimal TickSize) : SymbolFilter;

    [Immutable]
    public record PercentPriceSymbolFilter(decimal MultiplierUp, decimal MultiplierDown, int AvgPriceMins) : SymbolFilter;

    [Immutable]
    public record LotSizeSymbolFilter(decimal MinQuantity, decimal MaxQuantity, decimal StepSize) : SymbolFilter;

    [Immutable]
    public record MinNotionalSymbolFilter(decimal MinNotional, bool ApplyToMarket, int AvgPriceMins) : SymbolFilter;

    [Immutable]
    public record IcebergPartsSymbolFilter(int Limit) : SymbolFilter;

    [Immutable]
    public record MarketLotSizeSymbolFilter(decimal MinQuantity, decimal MaxQuantity, decimal StepSize) : SymbolFilter;

    [Immutable]
    public record MaxNumberOfOrdersSymbolFilter(int MaxNumberOfOrders) : SymbolFilter;

    [Immutable]
    public record MaxNumberOfAlgoOrdersSymbolFilter(int MaxNumberOfAlgoOrders) : SymbolFilter;

    [Immutable]
    public record MaxNumberOfIcebergOrdersSymbolFilter(int MaxNumberOfIcebergOrders) : SymbolFilter;

    [Immutable]
    public record MaxPositionSymbolFilter(decimal MaxPosition) : SymbolFilter;
}