namespace Outcompute.Trader.Trading.Indicators;

public record struct MacdValue
{
    public decimal Price { get; init; }
    public decimal Fast { get; init; }
    public decimal Slow { get; init; }
    public decimal Macd { get; init; }
    public decimal Signal { get; init; }
    public decimal Histogram { get; init; }

    public bool IsUptrend { get; init; }
    public bool IsDowntrend { get; init; }
    public bool IsNeutral { get; init; }

    public bool IsUpcross { get; init; }
    public bool IsDowncross { get; init; }

    public static MacdValue Empty => new MacdValue();
}