using Orleans.Concurrency;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

[Immutable]
public record class PositionStats
{
    public decimal TotalQuantity { get; init; }
    public decimal TotalCost { get; init; }
    public decimal AvgPrice { get; init; }
    public decimal PresentValue { get; init; }
    public decimal AbsolutePnL { get; init; }
    public decimal RelativePnL { get; init; }
    public decimal RelativeValue { get; init; }

    public static PositionStats Zero => new();

    public static Builder CreateBuilder() => new();

    public class Builder
    {
        internal Builder()
        {
        }

        public decimal TotalQuantity { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal PresentValue { get; set; }
        public decimal AbsolutePnL { get; set; }
        public decimal RelativePnL { get; set; }
        public decimal RelativeValue { get; set; }

        public PositionStats ToImmutable() => new()
        {
            TotalQuantity = TotalQuantity,
            TotalCost = TotalCost,
            AvgPrice = AvgPrice,
            PresentValue = PresentValue,
            AbsolutePnL = AbsolutePnL,
            RelativePnL = RelativePnL,
            RelativeValue = RelativeValue
        };
    }
}