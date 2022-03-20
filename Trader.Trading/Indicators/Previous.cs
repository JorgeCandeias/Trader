namespace Outcompute.Trader.Trading.Indicators;

public class Previous<T> : IndicatorBase<T, T>
{
    internal const int DefaultPeriods = 1;

    public Previous(IndicatorResult<T> source, int periods = DefaultPeriods)
        : base(source, true)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;

        Ready();
    }

    public int Periods { get; }

    protected override T Calculate(int index)
    {
        if (index < Periods)
        {
            return default!;
        }

        return Source[index - 1];
    }
}

public static partial class Indicator
{
    public static Previous<T> Previous<T>(this IndicatorResult<T> source, int periods = Indicators.Previous<T>.DefaultPeriods)
        => new(source, periods);
}