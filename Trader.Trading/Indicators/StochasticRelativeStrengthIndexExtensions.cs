namespace Outcompute.Trader.Trading.Indicators;

public record struct StochasticRelativeStrengthValue
{
    public decimal? K { get; init; }
    public decimal? D { get; init; }
}

public static class StochasticRelativeStrengthIndexExtensions
{
    public static IEnumerable<StochasticRelativeStrengthValue> StochasticRelativeStrengthIndex<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int smoothK = 3, int smoothD = 3, int lengthRsi = 14, int lengthStoch = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(smoothK, 1, nameof(smoothK));
        Guard.IsGreaterThanOrEqualTo(smoothD, 1, nameof(smoothD));
        Guard.IsGreaterThanOrEqualTo(lengthRsi, 1, nameof(lengthRsi));
        Guard.IsGreaterThanOrEqualTo(lengthStoch, 1, nameof(lengthStoch));

        var kf = source.Select(selector).RelativeStrengthIndex(lengthRsi).StochasticFunction(x => x, x => x, x => x, lengthStoch).Sma(smoothK);
        var df = kf.Sma(smoothD);

        var ke = kf.GetEnumerator();
        var de = df.GetEnumerator();

        while (ke.MoveNext() && de.MoveNext())
        {
            yield return new StochasticRelativeStrengthValue
            {
                K = ke.Current,
                D = de.Current
            };
        }
    }

    public static IEnumerable<StochasticRelativeStrengthValue> StochasticRelativeStrengthIndex(this IEnumerable<Kline> source, int smoothK = 3, int smoothD = 3, int lengthRsi = 14, int lengthStoch = 14)
    {
        return source.StochasticRelativeStrengthIndex(x => x.ClosePrice, smoothK, smoothD, lengthRsi, lengthStoch);
    }
}