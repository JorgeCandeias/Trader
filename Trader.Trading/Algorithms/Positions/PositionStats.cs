namespace Outcompute.Trader.Trading.Algorithms.Positions
{
    public record struct PositionStats
    {
        public decimal TotalQuantity { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal PresentValue { get; set; }
        public decimal AbsolutePnL { get; set; }
        public decimal RelativePnL { get; set; }
        public decimal RelativeValue { get; set; }

        public static PositionStats Zero => new();
    }
}