namespace Outcompute.Trader.Trading.Indicators;

public static class KlineExtensions
{
    public static OHLCV ToOHLCV(this Kline item) => new(item.OpenPrice, item.HighPrice, item.LowPrice, item.ClosePrice, item.Volume);

    public static HLC ToHLC(this Kline item) => new(item.HighPrice, item.LowPrice, item.ClosePrice);

    public static IEnumerable<OHLCV> ToOHLCV(this IEnumerable<Kline> source) => source.Select(x => x.ToOHLCV());

    public static IEnumerable<HLC> ToHLC(this IEnumerable<Kline> source) => source.Select(x => x.ToHLC());
}