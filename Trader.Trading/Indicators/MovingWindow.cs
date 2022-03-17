namespace Outcompute.Trader.Trading.Indicators;

public class MovingWindow<T> : IndicatorBase<T, IEnumerable<T>>
{
    internal const int DefaultPeriods = 1;

    public MovingWindow(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
    }

    public MovingWindow(IIndicatorResult<T> source, int periods = DefaultPeriods) : this(periods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
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
    public static MovingWindow<T> MovingWindow<T>(int periods = Indicators.MovingWindow<T>.DefaultPeriods) => new(periods);

    public static MovingWindow<T> MovingWindow<T>(IIndicatorResult<T> source, int periods = Indicators.MovingWindow<T>.DefaultPeriods) => new(source, periods);
}

public static class MovingWindowEnumerableExtensions
{
    /// <summary>
    /// Yields a moving window over <paramref name="source"/> of size <paramref name="length"/>.
    /// </summary>
    public static IEnumerable<IEnumerable<T>> MovingWindow<T>(this IEnumerable<T> source, int periods = Indicators.MovingWindow<T>.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));

        using var indicator = Indicator.MovingWindow<T>(periods);

        foreach (var item in source)
        {
            indicator.Add(item);

            yield return indicator[^1];
        }
    }
}