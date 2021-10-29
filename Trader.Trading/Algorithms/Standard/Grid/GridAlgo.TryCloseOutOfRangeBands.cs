using Outcompute.Trader.Trading.Commands;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        protected IAlgoCommand? TryCloseOutOfRangeBands()
        {
            // take the upper band
            var upper = _bands.Max;
            if (upper is null) return null;

            // calculate the step size
            var step = upper.OpenPrice * _options.PullbackRatio;

            // take the lower band
            var band = _bands.Min;
            if (band is null) return null;

            // ensure the lower band is on ordered status
            if (band.Status != BandStatus.Ordered) return null;

            // ensure the lower band is opening within reasonable range of the current price
            if (band.OpenPrice >= Context.Ticker.ClosePrice - step) return null;

            // if the above checks fails then close the band
            if (band.OpenOrderId is not 0)
            {
                return CancelOrder(Context.Symbol, band.OpenOrderId);
            }

            return null;
        }
    }
}