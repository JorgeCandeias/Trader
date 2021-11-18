using Orleans.Concurrency;

namespace Outcompute.Trader.Trading.Algorithms.Positions
{
    [Immutable]
    public record Stats(decimal AvgPerHourDay1, decimal AvgPerHourDay7, decimal AvgPerHourDay30, decimal AvgPerDay1, decimal AvgPerDay7, decimal AvgPerDay30)
    {
        public static Stats Zero { get; } = new Stats(0, 0, 0, 0, 0, 0);

        public static Stats FromProfit(Profit profit)
        {
            if (profit is null) throw new ArgumentNullException(nameof(profit));

            return new Stats(
                profit.D1 / 24m,
                profit.D7 / (24m * 7m),
                profit.D30 / (24m * 30m),
                profit.D1,
                profit.D7 / 7m,
                profit.D30 / 30m);
        }
    }
}