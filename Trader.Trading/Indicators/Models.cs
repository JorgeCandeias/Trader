namespace Outcompute.Trader.Trading.Indicators;

public record struct OHLCV(decimal? Open, decimal? High, decimal? Low, decimal? Close, decimal? Volume);

public record struct HLC(decimal? High, decimal? Low, decimal? Close);

public record struct HL(decimal? High, decimal? Low);

public static class ModelConversionExtensions
{
    public static HL ToHL(this OHLCV item) => new(item.High, item.Low);

    public static HLC ToHLC(this OHLCV item) => new(item.High, item.Low, item.Close);

    public static IEnumerable<HL> ToHL(this IEnumerable<OHLCV> source) => source.Select(x => x.ToHL());

    public static IEnumerable<HLC> ToHLC(this IEnumerable<OHLCV> source) => source.Select(x => x.ToHLC());
}