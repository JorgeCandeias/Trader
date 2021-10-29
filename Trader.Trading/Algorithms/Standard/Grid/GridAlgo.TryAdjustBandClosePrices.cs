using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Commands;
using System;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        protected IAlgoCommand? TryAdjustBandClosePrices()
        {
            // skip this step if there are no bands to adjust
            if (_bands.Count == 0)
            {
                return null;
            }

            // figure out the constant step size
            var stepSize = _bands.Max!.OpenPrice * _options.PullbackRatio;

            // adjust close prices on the bands
            foreach (var band in _bands)
            {
                band.ClosePrice = band.OpenPrice + stepSize;

                // ensure the close price is below the max percent filter
                // this can happen due to an asset crashing down several multiples
                var maxPrice = Context.Ticker.ClosePrice * Context.Symbol.Filters.PercentPrice.MultiplierUp;
                if (band.ClosePrice > maxPrice)
                {
                    _logger.LogError(
                        "{Type} {Name} detected band sell price for {Quantity} {Asset} of {Price} {Quote} is above the percent price filter of {MaxPrice} {Quote}",
                        TypeName, Context.Name, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, maxPrice, Context.Symbol.QuoteAsset);
                }

                // ensure the close price is above the min percent filter
                // this can happen to old leftovers that were bought very cheap
                var minPrice = Context.Ticker.ClosePrice * Context.Symbol.Filters.PercentPrice.MultiplierDown;
                if (band.ClosePrice < minPrice)
                {
                    _logger.LogWarning(
                        "{Type} {Name} adjusted sell of {Quantity} {Asset} for {ClosePrice} {Quote} to {MinPrice} {Quote} because it is below the percent price filter of {MinPrice} {Quote}",
                        TypeName, Context.Name, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, minPrice, Context.Symbol.QuoteAsset, minPrice, Context.Symbol.QuoteAsset);

                    band.ClosePrice = minPrice;
                }

                // adjust the sell price up to the tick size
                band.ClosePrice = Math.Ceiling(band.ClosePrice / Context.Symbol.Filters.Price.TickSize) * Context.Symbol.Filters.Price.TickSize;
            }

            // let the algo continue
            return null;
        }
    }
}