using Outcompute.Trader.Trading.Algorithms.Positions;

namespace System.Collections.Generic
{
    public static class PositionEnumerableExtensions
    {
        /// <inheritdoc cref="GetStatsCore(IEnumerable{(decimal Quantity, decimal Price)}, decimal)"/>
        public static PositionStats GetStats(this IEnumerable<Position> positions, decimal price)
        {
            var source = positions.Select(x => (x.Quantity, x.Price));

            return GetStatsCore(source, price);
        }

        /// <inheritdoc cref="GetStatsCore(IEnumerable{(decimal Quantity, decimal Price)}, decimal)"/>
        public static PositionStats GetStats(this IEnumerable<PositionLot> positions, decimal price)
        {
            var source = positions.Select(x => (x.Quantity, x.AvgPrice));

            return GetStatsCore(source, price);
        }

        /// <summary>
        /// Calculate relative profit and loss statistics in one pass vs the specified price.
        /// </summary>
        private static PositionStats GetStatsCore(IEnumerable<(decimal Quantity, decimal Price)> positions, decimal price)
        {
            if (positions is null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (positions.TryGetNonEnumeratedCount(out var count) && count is 0)
            {
                return PositionStats.Zero;
            }

            var builder = PositionStats.CreateBuilder();

            foreach (var item in positions)
            {
                builder.TotalQuantity += item.Quantity;
                builder.TotalCost += item.Quantity * item.Price;
                builder.PresentValue += item.Quantity * price;
            }

            builder.AvgPrice = builder.TotalQuantity is 0 ? 0 : builder.TotalCost / builder.TotalQuantity;
            builder.AbsolutePnL = builder.PresentValue - builder.TotalCost;
            builder.RelativePnL = builder.TotalCost is 0 ? 0 : builder.AbsolutePnL / builder.TotalCost;
            builder.RelativeValue = builder.TotalCost is 0 ? 0 : builder.PresentValue / builder.TotalCost;

            return builder.ToImmutable();
        }

        public static IEnumerable<PositionLot> EnumerateLots(this IEnumerable<Position> source, decimal size)
        {
            return new PositionLotIterator(source, size);
        }
    }
}