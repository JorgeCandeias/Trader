using Outcompute.Trader.Trading.Algorithms.Positions;

namespace System.Collections.Generic
{
    public static class PositionEnumerableExtensions
    {
        /// <summary>
        /// Calculate relative profit and loss statistics in one pass.
        /// </summary>
        public static PositionStats GetStats(this IEnumerable<Position> positions, decimal price)
        {
            if (positions is null) throw new ArgumentNullException(nameof(positions));

            if (positions.TryGetNonEnumeratedCount(out var count) && count is 0) return PositionStats.Zero;

            var stats = new PositionStats();

            foreach (var item in positions)
            {
                stats.TotalQuantity += item.Quantity;
                stats.TotalCost += item.Quantity * item.Price;
                stats.PresentValue += item.Quantity * price;
            }

            stats.AvgPrice = stats.TotalQuantity is 0 ? 0 : stats.TotalCost / stats.TotalQuantity;
            stats.AbsolutePnL = stats.PresentValue - stats.TotalCost;
            stats.RelativePnL = stats.TotalCost is 0 ? 0 : stats.AbsolutePnL / stats.TotalCost;
            stats.RelativeValue = stats.TotalCost is 0 ? 0 : stats.PresentValue / stats.TotalCost;

            return stats;
        }
    }
}