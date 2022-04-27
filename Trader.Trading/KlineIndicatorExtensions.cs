using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading;

public static class KlineIndicatorExtensions
{
    public static IEnumerable<BollingerBand> ToBollingerBands(this IEnumerable<Kline> source, int periods = BollingerBands.DefaultPeriods, int multiplier = BollingerBands.DefaultMultipler)
    {
        return source.ToBollingerBands(x => x.ClosePrice, periods, multiplier);
    }

    public static IEnumerable<decimal?> ToSma(this IEnumerable<Kline> source, int periods = Sma.DefaultPeriods)
    {
        return source.ToSma(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ToHL2(this IEnumerable<Kline> source)
    {
        return source.ToHL2(x => x.HighPrice, x => x.LowPrice);
    }

    public static IEnumerable<decimal?> ToAwesomeOscillator(this IEnumerable<Kline> source, int fastPeriods = AwesomeOscillator.DefaultFastPeriods, int slowPeriods = AwesomeOscillator.DefaultSlowPeriods)
    {
        return source.ToAwesomeOscillator(x => x.HighPrice, x => x.LowPrice, fastPeriods, slowPeriods);
    }

    public static IEnumerable<decimal?> ToEma(this IEnumerable<Kline> source, int periods = Ema.DefaultPeriods)
    {
        return source.ToEma(x => x.ClosePrice, periods);
    }

    public static IEnumerable<BBP> ToBullBearPower(this IEnumerable<Kline> source, int periods = BullBearPower.DefaultPeriods)
    {
        return source.ToBullBearPower(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ToVwma(this IEnumerable<Kline> source, int periods = Vwma.DefaultPeriods)
    {
        return source.ToVwma(x => x.ClosePrice, x => x.Volume, periods);
    }

    public static IEnumerable<decimal?> ToRma(this IEnumerable<Kline> source, int periods = Rma.DefaultPeriods)
    {
        return source.ToRma(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ToTrueRange(this IEnumerable<Kline> source, bool fallback = false)
    {
        return source.ToTrueRange(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, fallback);
    }

    public static IEnumerable<DMI> ToDmi(this IEnumerable<Kline> source, int adxPeriods = Dmi.DefaultAdxPeriods, int diPeriods = Dmi.DefaultDiPeriods)
    {
        return source.ToDmi(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, adxPeriods, diPeriods);
    }

    public static IEnumerable<decimal?> ToHighest(this IEnumerable<Kline> source, int periods = 1, bool outputWarmup = false)
    {
        return source.ToHighest(x => x.HighPrice, periods, outputWarmup);
    }

    public static IEnumerable<decimal?> ToLowest(this IEnumerable<Kline> source, int periods = 1, bool outputWarmup = false)
    {
        return source.ToLowest(x => x.LowPrice, periods, outputWarmup);
    }

    public static IEnumerable<IchimokuCloudResult> ToIchimokuCloud(this IEnumerable<Kline> source, int conversionPeriods = 9, int basePeriods = 26, int laggingSpan2Periods = 52, int displacement = 26)
    {
        return source.ToIchimokuCloud(x => x.HighPrice, x => x.LowPrice, conversionPeriods, basePeriods, laggingSpan2Periods, displacement);
    }

    public static IEnumerable<KdjValue> ToKdj(this IEnumerable<Kline> source, int periods = Kdj.DefaultPeriods, int ma1 = Kdj.DefaultMa1, int ma2 = Kdj.DefaultMa2)
    {
        return source.Select(x => new HLC(x.HighPrice, x.LowPrice, x.ClosePrice)).ToKdj(periods, ma1, ma2);
    }

    public static IEnumerable<MacdResult> ToMacd(this IEnumerable<Kline> source, int fastPeriods = Macd.DefaultFastPeriods, int slowPeriods = Macd.DefaultSlowPeriods, int signalPeriods = Macd.DefaultSignalPeriods)
    {
        return source.Select(x => (decimal?)x.ClosePrice).Identity().Macd(fastPeriods, slowPeriods, signalPeriods);
    }

    public static IEnumerable<decimal?> ToMomentum(this IEnumerable<Kline> source, int periods = Momentum.DefaultPeriods)
    {
        return source.ToMomentum(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ToRsi(this IEnumerable<Kline> source, int periods = 14)
    {
        return source.ToRsi(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ToSmaDev(this IEnumerable<Kline> source, int periods = SmaDev.DefaultPeriods)
    {
        return source.ToSmaDev(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ToStochastic(this IEnumerable<Kline> source, int periods = Stochastic.DefaultPeriods)
    {
        return source.ToStochastic(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, periods);
    }

    public static IEnumerable<StochasticOscillatorResult> ToStochasticOscillator(this IEnumerable<Kline> source, int periodsK = StochasticOscillator.DefaultPeriodsK, int smoothK = StochasticOscillator.DefaultSmoothK, int periodsD = StochasticOscillator.DefaultPeriodsD)
    {
        return source.ToHLC().Identity().StochasticOscillator(periodsK, smoothK, periodsD);
    }

    public static IEnumerable<TechnicalRatingSummary> ToTechnicalRatingsSummary(this IEnumerable<Kline> klines)
    {
        return klines.ToOHLCV().Identity().TechnicalRatings();
    }

    public static IEnumerable<decimal?> ToWma(this IEnumerable<Kline> source, int periods = Wma.DefaultPeriods)
    {
        return source.ToWma(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ToHma(this IEnumerable<Kline> source, int periods = Hma.DefaultPeriods)
    {
        return source.ToHma(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ToCci(this IEnumerable<Kline> source, int periods = Cci.DefaultPeriods)
    {
        return source.HLC3().Identity().Cci(periods);
    }

    public static IEnumerable<StochasticRsiResult> ToStochasticRsi(this IEnumerable<Kline> source, int smoothK = StochasticRsi.DefaultSmoothK, int smoothD = StochasticRsi.DefaultSmoothD, int periodsRsi = StochasticRsi.DefaultPeriodsRsi, int periodsStoch = StochasticRsi.DefaultPeriodsStoch)
    {
        return source.Select(x => (decimal?)x.ClosePrice).Identity().StochasticRsi(smoothK, smoothD, periodsRsi, periodsStoch);
    }

    public static IEnumerable<decimal?> ToWilliamsPercentRange(this IEnumerable<Kline> source, int periods = WilliamsPercentRange.DefaultPeriods)
    {
        return source.ToWilliamsPercentRange(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ToUltimateOscillator(
        this IEnumerable<Kline> source,
        int fastPeriods = UltimateOscillator.DefaultFastPeriods,
        int mediumPeriods = UltimateOscillator.DefaultMediumPeriods,
        int slowPeriods = UltimateOscillator.DefaultSlowPeriods)
    {
        return source.ToUltimateOscillator(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, fastPeriods, mediumPeriods, slowPeriods);
    }

    public static IEnumerable<decimal?> ToAtr(this IEnumerable<Kline> source, int periods = Atr.DefaultPeriods, AtrMethod method = Atr.DefaultAtrMethod)
    {
        return source.ToAtr(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, periods, method);
    }

    public static IEnumerable<decimal?> HLC3(this IEnumerable<Kline> source)
    {
        return source.ToHLC3(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice);
    }
}