using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public enum TechnicalRatingAction
{
    Unknown = 0,
    StrongSell = 1,
    Sell = 2,
    Neutral = 3,
    Buy = 4,
    StrongBuy = 5
}

public record TechnicalRatingDetail(string Indicator, decimal? Value, TechnicalRatingAction Status);

public record TechnicalRatingSignals(int Sell, int Neutral, int Buy)
{
    public static TechnicalRatingSignals Empty { get; } = new TechnicalRatingSignals(0, 0, 0);
}

public record TechnicalRatingTotals(decimal Rating, TechnicalRatingAction Action, TechnicalRatingSignals Signals)
{
    public static TechnicalRatingTotals Empty { get; } = new TechnicalRatingTotals(0, TechnicalRatingAction.Unknown, TechnicalRatingSignals.Empty);
}

public record TechnicalRatingSummary(OHLCV Item, TechnicalRatingTotals Summary, TechnicalRatingTotals MovingAverages, TechnicalRatingTotals Oscillators, ImmutableList<TechnicalRatingDetail> Details)
{
    public static TechnicalRatingSummary Empty { get; } = new TechnicalRatingSummary(OHLCV.Empty, TechnicalRatingTotals.Empty, TechnicalRatingTotals.Empty, TechnicalRatingTotals.Empty, ImmutableList<TechnicalRatingDetail>.Empty);
}

public static partial class Indicator
{
    public static TechnicalRatings TechnicalRatings(this IndicatorResult<OHLCV> source)
        => new(source);

    public static IEnumerable<TechnicalRatingSummary> ToTechnicalRatingsSummary(this IEnumerable<Kline> klines)
        => klines.ToOHLCV().Identity().TechnicalRatings();
}

public class TechnicalRatings : IndicatorBase<OHLCV, TechnicalRatingSummary>
{
    private const decimal StrongBound = 0.5M;
    private const decimal WeakBound = 0.1M;

    #region Shared

    private readonly IndicatorResult<decimal?> _close;
    private readonly IndicatorResult<decimal?> _prev;
    private readonly IndicatorResult<bool> _uptrend;
    private readonly IndicatorResult<bool> _downtrend;

    #endregion Shared

    #region Moving Averages

    private readonly Sma _sma10;
    private readonly Sma _sma20;
    private readonly Sma _sma30;
    private readonly Sma _sma50;
    private readonly Sma _sma100;
    private readonly Sma _sma200;
    private readonly Ema _ema10;
    private readonly Ema _ema20;
    private readonly Ema _ema30;
    private readonly Ema _ema50;
    private readonly Ema _ema100;
    private readonly Ema _ema200;
    private readonly Hma _hma9;
    private readonly Vwma _vwma20;
    private readonly IchimokuCloud _ichimoku;

    #endregion Moving Averages

    #region Oscillators

    private readonly Rsi _rsi;
    private readonly StochasticOscillator _stoch;
    private readonly Cci _cci;
    private readonly Dmi _dmi;
    private readonly AwesomeOscillator _ao;
    private readonly MovingWindow<decimal?> _aomw;
    private readonly Momentum _mom;
    private readonly Macd _macd;
    private readonly StochasticRsi _srsi;
    private readonly WilliamsPercentRange _wpr;
    private readonly BullBearPower _bbp;
    private readonly UltimateOscillator _uo;

    #endregion Oscillators

    public TechnicalRatings(IndicatorResult<OHLCV> source)
        : base(source, true)
    {
        // shared
        _close = source.Transform(x => x.Close);
        _prev = _close.Previous();
        var cv = source.Transform(x => x.ToCV());
        var hl = source.Transform(x => x.ToHL());
        var hlc = source.Transform(x => x.ToHLC());

        // moving averages
        _sma10 = Indicator.Sma(_close, 10);
        _sma20 = Indicator.Sma(_close, 20);
        _sma30 = Indicator.Sma(_close, 30);
        _sma50 = Indicator.Sma(_close, 50);
        _sma100 = Indicator.Sma(_close, 100);
        _sma200 = Indicator.Sma(_close, 200);
        _ema10 = Indicator.Ema(_close, 10);
        _ema20 = Indicator.Ema(_close, 20);
        _ema30 = Indicator.Ema(_close, 30);
        _ema50 = Indicator.Ema(_close, 50);
        _ema100 = Indicator.Ema(_close, 100);
        _ema200 = Indicator.Ema(_close, 200);
        _hma9 = Indicator.Hma(_close, 9);
        _vwma20 = Indicator.Vwma(cv, 20);
        _ichimoku = Indicator.IchimokuCloud(hl, 9, 26, 52, 26);

        // oscillators
        _rsi = Indicator.Rsi(_close, 14);
        _stoch = Indicator.StochasticOscillator(hlc, 14, 3, 3);
        _cci = Indicator.Cci(_close, 20);
        _dmi = Indicator.Dmi(hlc, 14, 14);
        _ao = Indicator.AwesomeOscillator(hl, 5, 34);
        _aomw = Indicator.MovingWindow(_ao, 3);
        _mom = Indicator.Momentum(_close, 10);
        _macd = Indicator.Macd(_close, 12, 26, 9);
        _srsi = Indicator.StochasticRsi(_close, 3, 3, 14, 14);
        _wpr = Indicator.WilliamsPercentRange(hlc, 14);
        _bbp = Indicator.BullBearPower(hlc, 13);
        _uo = Indicator.UltimateOscillator(hlc, 7, 14, 28);

        // recommendations
        var priceAvg = _ema50;
        _downtrend = Indicator.Zip(_close, priceAvg, (c, a) => c < a);
        _uptrend = Indicator.Zip(_close, priceAvg, (c, a) => c > a);

        Ready();
    }

    protected override TechnicalRatingSummary Calculate(int index)
    {
        if (index < 1)
        {
            return TechnicalRatingSummary.Empty;
        }

        // moving averages
        var averagesRating = 0M;
        var averagesRatingsCount = 0;
        var averagesSellSignals = 0;
        var averagesNeutralSignals = 0;
        var averagesBuySignals = 0;

        void ApplyMovingAveragesRating(int? rating)
        {
            if (rating.HasValue)
            {
                averagesRating += rating.Value;
                averagesRatingsCount++;
                if (rating > 0)
                {
                    averagesBuySignals++;
                }
                else if (rating < 0)
                {
                    averagesSellSignals++;
                }
                else
                {
                    averagesNeutralSignals++;
                }
            }
        }

        var sma10Rating = GetMovingAverageRating(_sma10[index], _close[index]);
        ApplyMovingAveragesRating(sma10Rating);

        var sma20Rating = GetMovingAverageRating(_sma20[index], _close[index]);
        ApplyMovingAveragesRating(sma20Rating);

        var sma30Rating = GetMovingAverageRating(_sma30[index], _close[index]);
        ApplyMovingAveragesRating(sma30Rating);

        var sma50Rating = GetMovingAverageRating(_sma50[index], _close[index]);
        ApplyMovingAveragesRating(sma50Rating);

        var sma100Rating = GetMovingAverageRating(_sma100[index], _close[index]);
        ApplyMovingAveragesRating(sma100Rating);

        var sma200Rating = GetMovingAverageRating(_sma200[index], _close[index]);
        ApplyMovingAveragesRating(sma200Rating);

        var ema10Rating = GetMovingAverageRating(_ema10[index], _close[index]);
        ApplyMovingAveragesRating(ema10Rating);

        var ema20Rating = GetMovingAverageRating(_ema20[index], _close[index]);
        ApplyMovingAveragesRating(ema20Rating);

        var ema30Rating = GetMovingAverageRating(_ema30[index], _close[index]);
        ApplyMovingAveragesRating(ema30Rating);

        var ema50Rating = GetMovingAverageRating(_ema50[index], _close[index]);
        ApplyMovingAveragesRating(ema50Rating);

        var ema100Rating = GetMovingAverageRating(_ema100[index], _close[index]);
        ApplyMovingAveragesRating(ema100Rating);

        var ema200Rating = GetMovingAverageRating(_ema200[index], _close[index]);
        ApplyMovingAveragesRating(ema200Rating);

        var hma9Rating = GetMovingAverageRating(_hma9[index], _close[index]);
        ApplyMovingAveragesRating(hma9Rating);

        var vwma20Rating = GetMovingAverageRating(_vwma20[index], _close[index]);
        ApplyMovingAveragesRating(vwma20Rating);

        var ichimokuRating = GetIchimokuCloudRating(_ichimoku[index], _close[index], _prev[index]);
        ApplyMovingAveragesRating(ichimokuRating);

        // average out the rating
        averagesRating = averagesRatingsCount > 0 ? averagesRating / averagesRatingsCount : 0;

        // oscillators
        var oscillatorsRating = 0M;
        var oscillatorsRatingsCount = 0;
        var oscillatorsSellSignals = 0;
        var oscillatorsNeutralSignals = 0;
        var oscillatorsBuySignals = 0;

        void ApplyOscillatorRating(int? rating)
        {
            if (rating.HasValue)
            {
                oscillatorsRating += rating.Value;
                oscillatorsRatingsCount++;
                if (rating > 0)
                {
                    oscillatorsBuySignals++;
                }
                else if (rating < 0)
                {
                    oscillatorsSellSignals++;
                }
                else
                {
                    oscillatorsNeutralSignals++;
                }
            }
        }

        var rsiRating = GetRsiRating(index);
        ApplyOscillatorRating(rsiRating);

        var stochRating = GetStochasticRating(index);
        ApplyOscillatorRating(stochRating);

        var cciRating = GetCciRating(index);
        ApplyOscillatorRating(cciRating);

        var adxRating = GetAdxRating(index);
        ApplyOscillatorRating(adxRating);

        var aoRating = GetAwesomeOscilatorRating(index);
        ApplyOscillatorRating(aoRating);

        var momRating = GetMomentumRating(index);
        ApplyOscillatorRating(momRating);

        var macdRating = GetMacdRating(index);
        ApplyOscillatorRating(macdRating);

        var stochRsiRating = GetStochRsiRating(index);
        ApplyOscillatorRating(stochRsiRating);

        var wprRating = GetWilliamsPercentRangeRating(index);
        ApplyOscillatorRating(wprRating);

        var bbpRating = GetBullBearPowerRating(index);
        ApplyOscillatorRating(bbpRating);

        var uoRating = GetUltimateOscillatorRating(index);
        ApplyOscillatorRating(uoRating);

        // average out the oscillators rating
        oscillatorsRating = oscillatorsRatingsCount > 0 ? oscillatorsRating / oscillatorsRatingsCount : 0;

        // add ratings up to the summary
        var summaryRating = 0M;
        var summaryRatingsCount = 0;
        var summarySellSignals = 0;
        var summaryNeutralSignals = 0;
        var summaryBuySignals = 0;

        if (averagesRatingsCount > 0)
        {
            summaryRating += averagesRating;
            summarySellSignals += averagesSellSignals;
            summaryNeutralSignals += averagesNeutralSignals;
            summaryBuySignals += averagesBuySignals;
            summaryRatingsCount++;
        }

        if (oscillatorsRatingsCount > 0)
        {
            summaryRating += oscillatorsRating;
            summarySellSignals += oscillatorsSellSignals;
            summaryNeutralSignals += oscillatorsNeutralSignals;
            summaryBuySignals += oscillatorsBuySignals;
            summaryRatingsCount++;
        }

        summaryRating = summaryRatingsCount > 0 ? summaryRating / summaryRatingsCount : 0;

        return new TechnicalRatingSummary(
            Source[index],
            new TechnicalRatingTotals(summaryRating, GetRatingStatus(summaryRating), new TechnicalRatingSignals(summarySellSignals, summaryNeutralSignals, summaryBuySignals)),
            new TechnicalRatingTotals(averagesRating, GetRatingStatus(averagesRating), new TechnicalRatingSignals(averagesSellSignals, averagesNeutralSignals, averagesBuySignals)),
            new TechnicalRatingTotals(oscillatorsRating, GetRatingStatus(oscillatorsRating), new TechnicalRatingSignals(oscillatorsSellSignals, oscillatorsNeutralSignals, oscillatorsBuySignals)),
            ImmutableList.Create<TechnicalRatingDetail>(
                new TechnicalRatingDetail("Relative Strength Index (14)", _rsi[index], GetIndividualRatingStatus(rsiRating)),
                new TechnicalRatingDetail("Stochastic %K (14, 3, 3)", _stoch[index].K, GetIndividualRatingStatus(stochRating)),
                new TechnicalRatingDetail("Commodity Channel Index (20)", _cci[index], GetIndividualRatingStatus(cciRating)),
                new TechnicalRatingDetail("Average Directional Index (14)", _dmi[index].Adx, GetIndividualRatingStatus(adxRating)),
                new TechnicalRatingDetail("Awesome Oscillator", _ao[index], GetIndividualRatingStatus(aoRating)),
                new TechnicalRatingDetail("Momentum (10)", _mom[index], GetIndividualRatingStatus(momRating)),
                new TechnicalRatingDetail("MACD Level (12, 26)", _macd[index].Macd, GetIndividualRatingStatus(macdRating)),
                new TechnicalRatingDetail("Stochastic RSI Fast (3, 3, 14, 14)", _srsi[index].K, GetIndividualRatingStatus(stochRsiRating)),
                new TechnicalRatingDetail("Williams Percentage Range (14)", _wpr[index], GetIndividualRatingStatus(wprRating)),
                new TechnicalRatingDetail("Bull Bear Power", _bbp[index].Power, GetIndividualRatingStatus(bbpRating)),
                new TechnicalRatingDetail("Ultimate Oscillator (7, 14, 28)", _uo[index], GetIndividualRatingStatus(uoRating)),
                new TechnicalRatingDetail("Exponential Moving Average (10)", _ema10[index], GetIndividualRatingStatus(ema10Rating)),
                new TechnicalRatingDetail("Simple Moving Average (10)", _sma10[index], GetIndividualRatingStatus(sma10Rating)),
                new TechnicalRatingDetail("Exponential Moving Average (20)", _ema20[index], GetIndividualRatingStatus(ema20Rating)),
                new TechnicalRatingDetail("Simple Moving Average (20)", _sma20[index], GetIndividualRatingStatus(sma20Rating)),
                new TechnicalRatingDetail("Exponential Moving Average (30)", _ema30[index], GetIndividualRatingStatus(ema30Rating)),
                new TechnicalRatingDetail("Simple Moving Average (30)", _sma30[index], GetIndividualRatingStatus(sma30Rating)),
                new TechnicalRatingDetail("Exponential Moving Average (50)", _ema50[index], GetIndividualRatingStatus(ema50Rating)),
                new TechnicalRatingDetail("Simple Moving Average (50)", _sma50[index], GetIndividualRatingStatus(sma50Rating)),
                new TechnicalRatingDetail("Exponential Moving Average (100)", _ema100[index], GetIndividualRatingStatus(ema100Rating)),
                new TechnicalRatingDetail("Simple Moving Average (100)", _sma100[index], GetIndividualRatingStatus(sma100Rating)),
                new TechnicalRatingDetail("Exponential Moving Average (200)", _ema200[index], GetIndividualRatingStatus(ema200Rating)),
                new TechnicalRatingDetail("Simple Moving Average (200)", _sma200[index], GetIndividualRatingStatus(sma200Rating)),
                new TechnicalRatingDetail("Ichimoku Base Line (9, 26, 52, 26)", _ichimoku[index].BaseLine, GetIndividualRatingStatus(ichimokuRating)),
                new TechnicalRatingDetail("Volume Weighted Moving Average (20)", _vwma20[index], GetIndividualRatingStatus(vwma20Rating)),
                new TechnicalRatingDetail("Hull Moving Average (9)", _hma9[index], GetIndividualRatingStatus(hma9Rating))));
    }

    private static int? GetIchimokuCloudRating(IchimokuCloudResult item, decimal? close, decimal? prev)
    {
        if (item.ConversionLine.HasValue && item.BaseLine.HasValue && item.LeadLine1.HasValue && item.LeadLine2.HasValue && close.HasValue && prev.HasValue)
        {
            var buy = item.LeadLine1 > item.LeadLine2
                && close > item.LeadLine1
                && close < item.BaseLine
                && prev < item.ConversionLine
                && close > item.ConversionLine;

            var sell = item.LeadLine2 > item.LeadLine1
                && close < item.LeadLine2
                && close > item.BaseLine
                && prev > item.ConversionLine
                && close < item.ConversionLine;

            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetCciRating(int index)
    {
        var curr = _cci[index];
        var prev = _cci[index - 1];

        if (curr.HasValue && prev.HasValue)
        {
            var buy = curr < -100 && curr > prev;
            var sell = curr > 100 && curr < prev;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetStochasticRating(int index)
    {
        var currK = _stoch[index].K;
        var currD = _stoch[index].D;
        var prevK = _stoch[index - 1].K;
        var prevD = _stoch[index - 1].D;

        if (currK.HasValue && currD.HasValue && prevK.HasValue && prevD.HasValue)
        {
            var buy = currK < 20 && currD < 20 && currK > currD && prevK < prevD;
            var sell = currK > 80 && currD > 80 && currK < currD && prevK > prevD;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetRsiRating(int index)
    {
        var curr = _rsi[index];
        var prev = _rsi[index - 1];

        if (curr.HasValue && prev.HasValue)
        {
            var buy = curr < 30 && prev < curr;
            var sell = curr > 70 && prev > curr;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetUltimateOscillatorRating(int index)
    {
        var value = _uo[index];

        if (value.HasValue)
        {
            var buy = value > 70;
            var sell = value < 30;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetBullBearPowerRating(int index)
    {
        var curr = _bbp[index];
        var prev = _bbp[index - 1];
        var uptrend = _uptrend[index];
        var downtrend = _downtrend[index];

        if (curr.BullPower.HasValue && curr.BearPower.HasValue && prev.BullPower.HasValue && prev.BearPower.HasValue)
        {
            var buy = uptrend && curr.BearPower < 0 && curr.BullPower > prev.BearPower;
            var sell = downtrend && curr.BullPower > 0 && curr.BullPower < prev.BullPower;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetWilliamsPercentRangeRating(int index)
    {
        var curr = _wpr[index];
        var prev = _wpr[index - 1];

        if (curr.HasValue && prev.HasValue)
        {
            var buy = curr < -80 && curr > prev;
            var sell = curr > -20 && curr < prev;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetStochRsiRating(int index)
    {
        var currK = _srsi[index].K;
        var currD = _srsi[index].D;
        var prevK = _srsi[index - 1].K;
        var prevD = _srsi[index - 1].D;
        var uptrend = _uptrend[index];
        var downtrend = _downtrend[index];

        if (currK.HasValue && currD.HasValue && prevK.HasValue && prevD.HasValue)
        {
            var buy = downtrend && currK < 20 && currD < 20 && currK > currD && prevK < prevD;
            var sell = uptrend && currK > 80 && currD > 80 && currK < currD && prevK > prevD;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetMacdRating(int index)
    {
        var item = _macd[index];

        if (item.Macd.HasValue && item.Signal.HasValue)
        {
            var buy = item.Macd > item.Signal;
            var sell = item.Macd < item.Signal;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetMomentumRating(int index)
    {
        var curr = _mom[index];
        var prev = _mom[index - 1];

        if (curr.HasValue && prev.HasValue)
        {
            var buy = curr > prev;
            var sell = curr < prev;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetAdxRating(int index)
    {
        var currAdx = _dmi[index].Adx;
        var currPlus = _dmi[index].Plus;
        var currMinus = _dmi[index].Minus;
        var prevPlus = _dmi[index - 1].Plus;
        var prevMinus = _dmi[index - 1].Minus;

        if (currAdx.HasValue && currPlus.HasValue && currMinus.HasValue && prevPlus.HasValue && prevMinus.HasValue)
        {
            var buy = currAdx > 20 && prevPlus < prevMinus && currPlus > currMinus;
            var sell = currAdx > 20 && prevPlus > prevMinus && currPlus < currMinus;
            return GetRating(buy, sell);
        }

        return null;
    }

    private int? GetAwesomeOscilatorRating(int index)
    {
        var list = _aomw[index].ToList();

        if (list.Count == 3)
        {
            var old = list[0];
            var prev = list[1];
            var curr = list[2];

            if (curr.HasValue && prev.HasValue && old.HasValue)
            {
                var buy = (curr > 0 && prev <= 0) || (curr > 0 && prev > 0 && curr > prev && old > prev);
                var sell = (curr < 0 && prev >= 0) || (curr < 0 && prev < 0 && curr < prev && old < prev);
                return GetRating(buy, sell);
            }
        }

        return null;
    }

    private static int? GetMovingAverageRating(decimal? ma, decimal? source)
    {
        if (ma.HasValue && source.HasValue)
        {
            if (ma < source) return 1;
            if (ma > source) return -1;
            return 0;
        }

        return null;
    }

    private static int GetRating(bool buy, bool sell)
    {
        if (buy) return 1;
        if (sell) return -1;

        return 0;
    }

    private static TechnicalRatingAction GetRatingStatus(decimal? rating)
    {
        if (rating.HasValue)
        {
            if (rating < -StrongBound)
            {
                return TechnicalRatingAction.StrongSell;
            }
            else if (rating < -WeakBound)
            {
                return TechnicalRatingAction.Sell;
            }
            else if (rating > StrongBound)
            {
                return TechnicalRatingAction.StrongBuy;
            }
            else if (rating > WeakBound)
            {
                return TechnicalRatingAction.Buy;
            }
            return TechnicalRatingAction.Neutral;
        }

        return TechnicalRatingAction.Unknown;
    }

    private static TechnicalRatingAction GetIndividualRatingStatus(int? rating)
    {
        if (rating.HasValue)
        {
            if (rating == 1)
            {
                return TechnicalRatingAction.Buy;
            }
            else if (rating == -1)
            {
                return TechnicalRatingAction.Sell;
            }
            else
            {
                return TechnicalRatingAction.Neutral;
            }
        }

        return TechnicalRatingAction.Unknown;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sma10.Dispose();
            _sma20.Dispose();
            _sma30.Dispose();
            _sma50.Dispose();
            _sma100.Dispose();
            _sma200.Dispose();
            _ema10.Dispose();
            _ema20.Dispose();
            _ema30.Dispose();
            _ema50.Dispose();
            _ema100.Dispose();
            _ema200.Dispose();
            _hma9.Dispose();
            _vwma20.Dispose();
            _ichimoku.Dispose();
            _rsi.Dispose();
            _stoch.Dispose();
            _cci.Dispose();
            _dmi.Dispose();
            _ao.Dispose();
            _aomw.Dispose();
            _mom.Dispose();
            _macd.Dispose();
            _srsi.Dispose();
            _wpr.Dispose();
            _bbp.Dispose();
            _uo.Dispose();
            _uptrend.Dispose();
            _downtrend.Dispose();
        }

        base.Dispose(disposing);
    }
}

public static class TechnicalRatingsEnumerableExtensions
{
    public static bool TryGetTechnicalRatingsSummaryUp(this IEnumerable<Kline> source, out TechnicalRatingSummary result, TechnicalRatingAction target = TechnicalRatingAction.Buy, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(iterations, 1, nameof(iterations));

        result = TechnicalRatingSummary.Empty;

        var root = source.ToOHLCV().Identity();

        // ensure there is enough data
        if (root.Count < 1)
        {
            return false;
        }

        var indicator = Indicator.TechnicalRatings(root);

        // the last summary must not be in buy action already
        if (indicator[^1].Summary.Action >= target)
        {
            return false;
        }

        // define the upper search range
        var high = source.Max(x => x.HighPrice) * 2M;
        if (high <= 0)
        {
            return false;
        }

        // define the lower search range
        var low = source.Min(x => x.LowPrice) / 2M;
        if (low <= 0)
        {
            return false;
        }

        // keep the last original data point as a template
        var template = root[^1];

        // perform binary search
        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateKline = template with
            {
                Close = candidatePrice,
                High = MathN.Max(template.High, candidatePrice),
                Low = MathN.Min(template.Low, candidatePrice)
            };

            // apply to the root
            root.Update(root.Count - 1, candidateKline);

            // get the updated indicator
            var candidateSummary = indicator[^1];

            // adjust ranges to search for a better candidate
            if (candidateSummary.Summary.Action >= target)
            {
                result = candidateSummary;
                high = candidatePrice;
            }
            else if (candidateSummary.Summary.Action < target)
            {
                low = candidatePrice;
            }
            else
            {
                result = candidateSummary;
                return true;
            }
        }

        return result != TechnicalRatingSummary.Empty;
    }

    public static bool TryGetTechnicalRatingsSummaryDown(this IEnumerable<Kline> source, out TechnicalRatingSummary result, TechnicalRatingAction target = TechnicalRatingAction.Buy, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(iterations, 1, nameof(iterations));

        result = TechnicalRatingSummary.Empty;

        var root = source.ToOHLCV().Identity();

        // ensure there is enough data
        if (root.Count < 1)
        {
            return false;
        }

        var indicator = Indicator.TechnicalRatings(root);

        // the last summary must not be in sell action already
        if (indicator[^1].Summary.Action <= target)
        {
            return false;
        }

        // define the upper search range
        var high = source.Max(x => x.HighPrice) * 2M;
        if (high <= 0)
        {
            return false;
        }

        // define the lower search range
        var low = source.Min(x => x.LowPrice) / 2M;
        if (low <= 0)
        {
            return false;
        }

        // keep the last original data point as a template
        var template = root[^1];

        // perform binary search
        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateKline = template with
            {
                Close = candidatePrice,
                High = MathN.Max(template.High, candidatePrice),
                Low = MathN.Min(template.Low, candidatePrice)
            };

            // apply to the root
            root.Update(root.Count - 1, candidateKline);

            // get the updated indicator
            var candidateSummary = indicator[^1];

            // adjust ranges to search for a better candidate
            if (candidateSummary.Summary.Action > target)
            {
                high = candidatePrice;
            }
            else if (candidateSummary.Summary.Action <= target)
            {
                result = candidateSummary;
                low = candidatePrice;
            }
            else
            {
                result = candidateSummary;
                return true;
            }
        }

        return result != TechnicalRatingSummary.Empty;
    }
}