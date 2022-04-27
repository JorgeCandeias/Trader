namespace Outcompute.Trader.Indicators;

public class FillNull<T> : IndicatorBase<T, T>
{
    public FillNull(IndicatorResult<T> source) : base(source, true)
    {
        Ready();
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
    public static FillNull<T> FillNull<T>(this IndicatorResult<T> source)
        => new(source);

    public static IEnumerable<T?> ToFillNull<T>(this IEnumerable<T?> source)
        => source.Identity().FillNull();
}