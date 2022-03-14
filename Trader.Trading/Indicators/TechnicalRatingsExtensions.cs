namespace Outcompute.Trader.Trading.Indicators;

public record struct TechnicalRatings();

public static class TechnicalRatingsExtensions
{
    public static TechnicalRatings TechnicalRatings(this IEnumerable<Kline> source)
    {
        return new();
    }

    private static void CalculateRatingAll(IEnumerable<Kline> klines)
    {
        /* moving averages */

        var sma10 = klines.SimpleMovingAverage(10);
        var sma20 = klines.SimpleMovingAverage(20);
        var sma30 = klines.SimpleMovingAverage(30);
        var sma50 = klines.SimpleMovingAverage(50);
        var sma100 = klines.SimpleMovingAverage(100);
        var sma200 = klines.SimpleMovingAverage(200);
        var ema10 = klines.ExponentialMovingAverage(10);
        var ema20 = klines.ExponentialMovingAverage(20);
        var ema30 = klines.ExponentialMovingAverage(30);
        var ema50 = klines.ExponentialMovingAverage(50);
        var ema100 = klines.ExponentialMovingAverage(100);
        var ema200 = klines.ExponentialMovingAverage(200);
        var hma9 = klines.HullMovingAverage(9);
        var vwma20 = klines.VolumeWeightedMovingAverage(20);
        var ichimoku = klines.IchimokuCloud();

        // other
        var rsi14 = klines.RelativeStrengthIndex(14);
        var stochastic = klines.StochasticOscillator(14, 3, 3);
        var cci = klines.Select(x => x.ClosePrice).CommodityChannelIndex(x => x, 20);
        var adx = klines.AverageDirectionalIndex(14, 14);
        var ao = klines.AwesomeOscillator(5, 34);
        var mom = klines.Momentum(10);
        var macd = klines.Macd(12, 26, 19);
        var stochRsi = klines.StochasticRelativeStrengthIndex(3, 3, 14, 14);
    }

    private static int? CalculateRatingMA(decimal? ma, decimal? source)
    {
        if (ma.HasValue && source.HasValue)
        {
            if (ma < source) return 1;
            if (ma > source) return -1;
            return 0;
        }

        return null;
    }

    private static int CalculateRating(bool buy, bool sell)
    {
        if (buy) return 1;
        if (sell) return -1;

        return 0;
    }
}