namespace Outcompute.Trader.Indicators;

public class MovingWindow<T> : IndicatorBase<T, IEnumerable<T>>
{
    internal const int DefaultPeriods = 1;
    internal const bool DefaultYieldPartialWindows = false;

    public MovingWindow(IndicatorResult<T> source, int periods = DefaultPeriods, bool yieldPartialWindows = DefaultYieldPartialWindows)
        : base(source, true)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        YieldPartialWindows = yieldPartialWindows;

        Ready();
    }

    public int Periods { get; }
    public bool YieldPartialWindows { get; }

    protected override IEnumerable<T> Calculate(int index)
    {
        if (index < Periods - 1 && !YieldPartialWindows)
        {
            yield break;
        }

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