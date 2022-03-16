namespace Outcompute.Trader.Trading.Indicators;

public record struct OHLCV(decimal? Open, decimal? High, decimal? Low, decimal? Close, decimal? Volume);

public record struct HLC(decimal? High, decimal? Low, decimal? Close);

public static class ModelConversionExtensions
{
    public static HLC ToHLC(this OHLCV item) => new HLC(item.High, item.Low, item.Close);

    public static IEnumerable<HLC> ToHLC(this IEnumerable<OHLCV> source) => source.Select(x => x.ToHLC());
}