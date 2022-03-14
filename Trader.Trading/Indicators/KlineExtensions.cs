namespace Outcompute.Trader.Trading.Indicators;

public static class KlineExtensions
{
    public static IEnumerable<decimal?> Close(this IEnumerable<Kline> source)
    {
        return source.Select(x => (decimal?)x.ClosePrice);
    }
}