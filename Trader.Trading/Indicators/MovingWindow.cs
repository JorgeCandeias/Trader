namespace Outcompute.Trader.Trading.Indicators;

public class MovingWindow<T> : IndicatorBase<T, IEnumerable<T>>
{
    internal const int DefaultPeriods = 1;

    public MovingWindow(IndicatorResult<T> source, int periods = DefaultPeriods)
        : base(source, true)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;

        Ready();
    }

    public int Periods { get; }

    protected override IEnumerable<T> Calculate(int index)
    {
        var start = Math.Max(index - Periods + 1, 0);
        var end = index + 1;

        for (var i = start; i < end; i++)
        {
            yield return Source[i];
        }
    }
}

public static partial class Indicator
{
    public static MovingWindow<T> MovingWindow<T>(this IndicatorResult<T> source, int periods = Indicators.MovingWindow<T>.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<IEnumerable<T>> ToMovingWindow<T>(this IEnumerable<T> source, int periods = Indicators.MovingWindow<T>.DefaultPeriods)
        => source.Identity().MovingWindow(periods);
}