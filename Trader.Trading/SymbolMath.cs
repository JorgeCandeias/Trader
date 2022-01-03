namespace System;

public static class SymbolMath
{
    public static decimal RaisePriceToTickSize(this Symbol symbol, decimal price)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Ceiling(price / symbol.Filters.Price.TickSize) * symbol.Filters.Price.TickSize;
    }

    public static decimal LowerPriceToTickSize(this Symbol symbol, decimal price)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Floor(price / symbol.Filters.Price.TickSize) * symbol.Filters.Price.TickSize;
    }

    public static decimal LowerQuantityToLotStepSize(this Symbol symbol, decimal quantity)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Floor(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;
    }

    public static decimal RaiseQuantityToLotStepSize(this Symbol symbol, decimal quantity)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Ceiling(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;
    }

    public static decimal RaiseQuantityToMinLotSize(this Symbol symbol, decimal quantity)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Max(quantity, symbol.Filters.LotSize.MinQuantity);
    }

    public static decimal RaiseTotalUpToMinNotional(this Symbol symbol, decimal total)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Max(total, symbol.Filters.MinNotional.MinNotional);
    }

    public static decimal LowerToBaseAssetPrecision(this Symbol symbol, decimal value)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Floor(value / symbol.BaseAssetPrecision) * symbol.BaseAssetPrecision;
    }

    public static decimal RaiseToBaseAssetPrecision(this Symbol symbol, decimal value)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Ceiling(value / symbol.BaseAssetPrecision) * symbol.BaseAssetPrecision;
    }

    public static decimal LowerToQuoteAssetPrecision(this Symbol symbol, decimal value)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Floor(value / symbol.QuoteAssetPrecision) * symbol.QuoteAssetPrecision;
    }

    public static decimal RaiseToQuoteAssetPrecision(this Symbol symbol, decimal value)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return Math.Ceiling(value / symbol.QuoteAssetPrecision) * symbol.QuoteAssetPrecision;
    }
}