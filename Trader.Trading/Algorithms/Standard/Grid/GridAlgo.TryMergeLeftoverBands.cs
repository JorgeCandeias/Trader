using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System.Linq;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        protected IAlgoCommand? TryMergeLeftoverBands()
        {
            // skip this rule if there are not enough bands to evaluate
            if (_bands.Count < 2)
            {
                return null;
            }

            // keep merging the lowest open band
            Band? merged = null;
            while (true)
            {
                // take the first two open bands
                var elected = _bands.Where(x => x.Status == BandStatus.Open).Take(2).ToArray();

                // break if there are less than two bands
                if (elected.Length < 2) break;

                // pin the bands
                var lowest = elected[0];
                var above = elected[1];

                // break if the lowest band is already above min lot size and min notional after adjustment
                if (lowest.Quantity.AdjustQuantityDownToLotStepSize(Context.Symbol) >= Context.Symbol.Filters.LotSize.MinQuantity &&
                    lowest.Quantity.AdjustQuantityDownToLotStepSize(Context.Symbol) * lowest.OpenPrice.AdjustPriceDownToTickSize(Context.Symbol) >= Context.Symbol.Filters.MinNotional.MinNotional)
                {
                    break;
                }

                // merge both bands
                merged = new Band
                {
                    Status = BandStatus.Open,
                    Quantity = lowest.Quantity + above.Quantity,
                    OpenPrice = (lowest.Quantity * lowest.OpenPrice + above.Quantity * above.OpenPrice) / (lowest.Quantity + above.Quantity),
                    OpenOrderId = lowest.OpenOrderId,
                    CloseOrderId = lowest.CloseOrderId,
                    CloseOrderClientId = lowest.CloseOrderClientId
                };

                // remove current bands
                _bands.Remove(lowest);
                _bands.Remove(above);

                // add the new merged band
                _bands.Add(merged);
            }

            // adjust the merged band
            if (merged is not null)
            {
                merged.Quantity = merged.Quantity.AdjustQuantityDownToLotStepSize(Context.Symbol);
            }

            // let the algo continue
            return null;
        }
    }
}