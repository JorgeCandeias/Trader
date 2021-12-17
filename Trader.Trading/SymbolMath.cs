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

    public static decimal AdjustQuantityDownToLotStepSize(this decimal quantity, Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return Math.Floor(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;
    }

    public static decimal AdjustQuantityUpToLotStepSize(this decimal quantity, Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return Math.Ceiling(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;
    }

    public static decimal AdjustQuantityUpToMinLotSizeQuantity(this decimal quantity, Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return Math.Max(quantity, symbol.Filters.LotSize.MinQuantity);
    }

    public static decimal AdjustTotalUpToMinNotional(this decimal total, Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return Math.Max(total, symbol.Filters.MinNotional.MinNotional);
    }

    public static decimal LowerToBaseAssetPrecision(this decimal value, Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return Math.Floor(value / symbol.BaseAssetPrecision) * symbol.BaseAssetPrecision;
    }

    public static decimal RaiseToBaseAssetPrecision(this decimal value, Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return Math.Ceiling(value / symbol.BaseAssetPrecision) * symbol.BaseAssetPrecision;
    }

    public static decimal LowerToQuoteAssetPrecision(this decimal value, Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return Math.Floor(value / symbol.QuoteAssetPrecision) * symbol.QuoteAssetPrecision;
    }

    public static decimal RaiseToQuoteAssetPrecision(this decimal value, Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return Math.Ceiling(value / symbol.QuoteAssetPrecision) * symbol.QuoteAssetPrecision;
    }
}