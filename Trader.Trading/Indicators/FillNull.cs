namespace Outcompute.Trader.Trading.Indicators;

public class FillNull<T> : IndicatorBase<T, T>
{
    public FillNull()
    {
    }

    public FillNull(IIndicatorResult<T> source) : this()
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    protected override T Calculate(int index)
    {
        if (Source[index] != null)
        {
            return Source[index];
        }

        for (var i = index - 1; i >= 0; i--)
        {
            if (Source[i] != null)
            {
                return Source[i];
            }
        }

        return default!;
    }
}

public static partial class Indicator
{
    public static FillNull<T> FillNull<T>() => new();

    public static FillNull<T> FillNull<T>(IIndicatorResult<T> source) => new(source);
}

public static class FillNullEnumerableExtensions
{
    public static IEnumerable<T?> FillNull<T>(this IEnumerable<T?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        using var indicator = Indicator.FillNull<T>();

        foreach (var item in source)
        {
            indicator.Add(item!);

            yield return indicator[^1];
        }
    }
}