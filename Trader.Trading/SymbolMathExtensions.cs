using System;

namespace Outcompute.Trader.Models
{
    public static class SymbolMathExtensions
    {
        public static decimal AdjustQuantityDownToLotSize(this Symbol symbol, decimal quantity)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return Math.Floor(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;
        }

        public static decimal AdjustQuantityUpToLotSize(this Symbol symbol, decimal quantity)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return Math.Ceiling(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;
        }

        public static decimal AdjustPriceDownToTickSize(this Symbol symbol, decimal price)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return Math.Floor(price / symbol.Filters.Price.TickSize) * symbol.Filters.Price.TickSize;
        }

        public static decimal AdjustPriceUpToTickSize(this Symbol symbol, decimal price)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return Math.Ceiling(price / symbol.Filters.Price.TickSize) * symbol.Filters.Price.TickSize;
        }

        public static decimal AdjustTotalUpToMinNotional(this Symbol symbol, decimal total)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return Math.Max(total, symbol.Filters.MinNotional.MinNotional);
        }
    }
}