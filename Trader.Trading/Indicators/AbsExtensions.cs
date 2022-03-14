namespace Outcompute.Trader.Trading.Indicators;

public static class AbsExtensions
{
    public static IEnumerable<decimal?> Abs(this IEnumerable<decimal?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        foreach (var value in source)
        {
            yield return value.HasValue ? Math.Abs(value.Value) : null;
        }
    }
}