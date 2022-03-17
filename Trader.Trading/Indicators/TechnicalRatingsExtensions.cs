namespace Outcompute.Trader.Trading.Indicators;

public record struct TechnicalRatings();

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

public record TechnicalRatingSummary(Kline Item, TechnicalRatingTotals Summary, TechnicalRatingTotals MovingAverages, TechnicalRatingTotals Oscillators, ImmutableList<TechnicalRatingDetail> Details)
{
    public static TechnicalRatingSummary Empty { get; } = new TechnicalRatingSummary(Kline.Empty, TechnicalRatingTotals.Empty, TechnicalRatingTotals.Empty, TechnicalRatingTotals.Empty, ImmutableList<TechnicalRatingDetail>.Empty);
}

public static class TechnicalRatingsExtensions
{
    private const decimal StrongBound = 0.5M;
    private const decimal WeakBound = 0.1M;

    public static IEnumerable<TechnicalRatingSummary> TechnicalRatingsSummary(this IEnumerable<Kline> klines)
    {
        // source
        var items = klines.GetEnumerator();

        // moving averages
        var sma10 = klines.Sma(10).GetEnumerator();
        var sma20 = klines.Sma(20).GetEnumerator();
        var sma30 = klines.Sma(30).GetEnumerator();
        var sma50 = klines.Sma(50).GetEnumerator();
        var sma100 = klines.Sma(100).GetEnumerator();
        var sma200 = klines.Sma(200).GetEnumerator();
        var ema10 = klines.Ema(10).GetEnumerator();
        var ema20 = klines.Ema(20).GetEnumerator();
        var ema30 = klines.Ema(30).GetEnumerator();
        var ema50 = klines.Ema(50).GetEnumerator();
        var ema100 = klines.Ema(100).GetEnumerator();
        var ema200 = klines.Ema(200).GetEnumerator();
        var hma9 = klines.Hma(9).GetEnumerator();
        var vwma20 = klines.VolumeWeightedMovingAverage(20).GetEnumerator();
        var ichimoku = klines.IchimokuCloud().GetEnumerator();

        // other
        var rsi14 = klines.RelativeStrengthIndex(14).WithPrevious().GetEnumerator();
        var stochastic = klines.StochasticOscillator(14, 3, 3).WithPrevious().GetEnumerator();
        var cci = klines.Select(x => x.ClosePrice).Cci(x => x, 20).WithPrevious().GetEnumerator();
        var adx = klines.Dmi(14, 14).WithPrevious().GetEnumerator();
        var ao = klines.AwesomeOscillator(5, 34).MovingWindow(3).GetEnumerator();
        var mom = klines.Momentum(10).WithPrevious().GetEnumerator();
        var macd = klines.Macd(12, 26, 19).GetEnumerator();
        var stochRsi = klines.StochasticRelativeStrengthIndex(3, 3, 14, 14).WithPrevious().GetEnumerator();
        var wpr = klines.WilliamsPercentRange(14).WithPrevious().GetEnumerator();
        var bbp = klines.BullBearPower(13).WithPrevious().GetEnumerator();
        var uo = klines.UltimateOscillator(7, 14, 28).GetEnumerator();

        // recommendations
        var priceAvg = klines.Ema(50);
        var downTrend = klines.Select(x => x.ClosePrice).Zip(priceAvg, (c, a) => c < a).GetEnumerator();
        var upTrend = klines.Select(x => x.ClosePrice).Zip(priceAvg, (c, a) => c > a).GetEnumerator();
        var close = klines.Select(x => x.ClosePrice).GetEnumerator();
        var prev = klines.Select(x => (decimal?)x.ClosePrice).Prepend(null).SkipLast(1).GetEnumerator();

        while (items.MoveNext() && close.MoveNext() && prev.MoveNext() && downTrend.MoveNext() && upTrend.MoveNext()
            && sma10.MoveNext() && sma20.MoveNext() && sma30.MoveNext() && sma50.MoveNext() && sma100.MoveNext() && sma200.MoveNext()
            && ema10.MoveNext() && ema20.MoveNext() && ema30.MoveNext() && ema50.MoveNext() && ema100.MoveNext() && ema200.MoveNext()
            && hma9.MoveNext() && vwma20.MoveNext() && ichimoku.MoveNext()
            && rsi14.MoveNext() && stochastic.MoveNext() && cci.MoveNext() && adx.MoveNext() && ao.MoveNext() && mom.MoveNext() && macd.MoveNext() && stochRsi.MoveNext()
            && wpr.MoveNext() && bbp.MoveNext() && uo.MoveNext())
        {
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

            var sma10Rating = GetMovingAverageRating(sma10.Current, close.Current);
            ApplyMovingAveragesRating(sma10Rating);

            var sma20Rating = GetMovingAverageRating(sma20.Current, close.Current);
            ApplyMovingAveragesRating(sma20Rating);

            var sma30Rating = GetMovingAverageRating(sma30.Current, close.Current);
            ApplyMovingAveragesRating(sma30Rating);

            var sma50Rating = GetMovingAverageRating(sma50.Current, close.Current);
            ApplyMovingAveragesRating(sma50Rating);

            var sma100Rating = GetMovingAverageRating(sma100.Current, close.Current);
            ApplyMovingAveragesRating(sma100Rating);

            var sma200Rating = GetMovingAverageRating(sma200.Current, close.Current);
            ApplyMovingAveragesRating(sma200Rating);

            var ema10Rating = GetMovingAverageRating(ema10.Current, close.Current);
            ApplyMovingAveragesRating(ema10Rating);

            var ema20Rating = GetMovingAverageRating(ema20.Current, close.Current);
            ApplyMovingAveragesRating(ema20Rating);

            var ema30Rating = GetMovingAverageRating(ema30.Current, close.Current);
            ApplyMovingAveragesRating(ema30Rating);

            var ema50Rating = GetMovingAverageRating(ema50.Current, close.Current);
            ApplyMovingAveragesRating(ema50Rating);

            var ema100Rating = GetMovingAverageRating(ema100.Current, close.Current);
            ApplyMovingAveragesRating(ema100Rating);

            var ema200Rating = GetMovingAverageRating(ema200.Current, close.Current);
            ApplyMovingAveragesRating(ema200Rating);

            var hma9Rating = GetMovingAverageRating(hma9.Current, close.Current);
            ApplyMovingAveragesRating(hma9Rating);

            var vwma20Rating = GetMovingAverageRating(vwma20.Current, close.Current);
            ApplyMovingAveragesRating(vwma20Rating);

            var ichimokuRating = GetIchimokuCloudRating(ichimoku.Current, close.Current, prev.Current);
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

            var rsiRating = GetRsiRating(rsi14.Current);
            ApplyOscillatorRating(rsiRating);

            var stochRating = GetStochasticRating(stochastic.Current);
            ApplyOscillatorRating(stochRating);

            var cciRating = GetCciRating(cci.Current);
            ApplyOscillatorRating(cciRating);

            var adxRating = GetAdxRating(adx.Current);
            ApplyOscillatorRating(adxRating);

            var aoRating = GetAwesomeOscilatorRating(ao.Current);
            ApplyOscillatorRating(aoRating);

            var momRating = GetMomentumRating(mom.Current);
            ApplyOscillatorRating(momRating);

            var macdRating = GetMacdRating(macd.Current);
            ApplyOscillatorRating(macdRating);

            var stochRsiRating = GetStochRsiRating(stochRsi.Current, upTrend.Current, downTrend.Current);
            ApplyOscillatorRating(stochRsiRating);

            var wprRating = GetWilliamsPercentRangeRating(wpr.Current);
            ApplyOscillatorRating(wprRating);

            var bbpRating = GetBullBearPowerRating(bbp.Current, upTrend.Current, downTrend.Current);
            ApplyOscillatorRating(bbpRating);

            var uoRating = GetUltimateOscillatorRating(uo.Current);
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

            // return the indicator
            yield return new TechnicalRatingSummary(
                items.Current,
                new TechnicalRatingTotals(summaryRating, GetRatingStatus(summaryRating), new TechnicalRatingSignals(summarySellSignals, summaryNeutralSignals, summaryBuySignals)),
                new TechnicalRatingTotals(averagesRating, GetRatingStatus(averagesRating), new TechnicalRatingSignals(averagesSellSignals, averagesNeutralSignals, averagesBuySignals)),
                new TechnicalRatingTotals(oscillatorsRating, GetRatingStatus(oscillatorsRating), new TechnicalRatingSignals(oscillatorsSellSignals, oscillatorsNeutralSignals, oscillatorsBuySignals)),
                ImmutableList.Create<TechnicalRatingDetail>(
                    new TechnicalRatingDetail("Relative Strength Index (14)", rsi14.Current.Current, GetIndividualRatingStatus(rsiRating)),
                    new TechnicalRatingDetail("Stochastic %K (14, 3, 3)", stochastic.Current.Current.K, GetIndividualRatingStatus(stochRating)),
                    new TechnicalRatingDetail("Commodity Channel Index (20)", cci.Current.Current, GetIndividualRatingStatus(cciRating)),
                    new TechnicalRatingDetail("Average Directional Index (14)", adx.Current.Current.Adx, GetIndividualRatingStatus(adxRating)),
                    new TechnicalRatingDetail("Awesome Oscillator", ao.Current.Last(), GetIndividualRatingStatus(aoRating)),
                    new TechnicalRatingDetail("Momentum (10)", mom.Current.Current, GetIndividualRatingStatus(momRating)),
                    new TechnicalRatingDetail("MACD Level (12, 26)", macd.Current.Macd, GetIndividualRatingStatus(macdRating)),
                    new TechnicalRatingDetail("Stochastic RSI Fast (3, 3, 14, 14)", stochRsi.Current.Current.K, GetIndividualRatingStatus(stochRsiRating)),
                    new TechnicalRatingDetail("Williams Percentage Range (14)", wpr.Current.Current, GetIndividualRatingStatus(wprRating)),
                    new TechnicalRatingDetail("Bull Bear Power", bbp.Current.Current.Power, GetIndividualRatingStatus(bbpRating)),
                    new TechnicalRatingDetail("Ultimate Oscillator (7, 14, 28)", uo.Current, GetIndividualRatingStatus(uoRating)),
                    new TechnicalRatingDetail("Exponential Moving Average (10)", ema10.Current, GetIndividualRatingStatus(ema10Rating)),
                    new TechnicalRatingDetail("Simple Moving Average (10)", sma10.Current, GetIndividualRatingStatus(sma10Rating)),
                    new TechnicalRatingDetail("Exponential Moving Average (20)", ema20.Current, GetIndividualRatingStatus(ema20Rating)),
                    new TechnicalRatingDetail("Simple Moving Average (20)", sma20.Current, GetIndividualRatingStatus(sma20Rating)),
                    new TechnicalRatingDetail("Exponential Moving Average (30)", ema30.Current, GetIndividualRatingStatus(ema30Rating)),
                    new TechnicalRatingDetail("Simple Moving Average (30)", sma30.Current, GetIndividualRatingStatus(sma30Rating)),
                    new TechnicalRatingDetail("Exponential Moving Average (50)", ema50.Current, GetIndividualRatingStatus(ema50Rating)),
                    new TechnicalRatingDetail("Simple Moving Average (50)", sma50.Current, GetIndividualRatingStatus(sma50Rating)),
                    new TechnicalRatingDetail("Exponential Moving Average (100)", ema100.Current, GetIndividualRatingStatus(ema100Rating)),
                    new TechnicalRatingDetail("Simple Moving Average (100)", sma100.Current, GetIndividualRatingStatus(sma100Rating)),
                    new TechnicalRatingDetail("Exponential Moving Average (200)", ema200.Current, GetIndividualRatingStatus(ema200Rating)),
                    new TechnicalRatingDetail("Simple Moving Average (200)", sma200.Current, GetIndividualRatingStatus(sma200Rating)),
                    new TechnicalRatingDetail("Ichimoku Base Line (9, 26, 52, 26)", ichimoku.Current.BaseLine, GetIndividualRatingStatus(ichimokuRating)),
                    new TechnicalRatingDetail("Volume Weighted Moving Average (20)", vwma20.Current, GetIndividualRatingStatus(vwma20Rating)),
                    new TechnicalRatingDetail("Hull Moving Average (9)", hma9.Current, GetIndividualRatingStatus(hma9Rating))));
        }
    }

    private static int? GetIchimokuCloudRating(IchimokuCloud item, decimal? close, decimal? prev)
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

    private static int? GetCciRating(WithPreviousValue<decimal?> item)
    {
        var curr = item.Current;
        var prev = item.Previous;

        if (curr.HasValue && prev.HasValue)
        {
            var buy = curr < -100 && curr > prev;
            var sell = curr > 100 && curr < prev;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetStochasticRating(WithPreviousValue<StochasticOscillatorValue> item)
    {
        var currK = item.Current.K;
        var currD = item.Current.D;
        var prevK = item.Previous.K;
        var prevD = item.Previous.D;

        if (currK.HasValue && currD.HasValue && prevK.HasValue && prevD.HasValue)
        {
            var buy = currK < 20 && currD < 20 && currK > currD && prevK < prevD;
            var sell = currK > 80 && currD > 80 && currK < currD && prevK > prevD;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetRsiRating(WithPreviousValue<decimal?> item)
    {
        var curr = item.Current;
        var prev = item.Previous;

        if (curr.HasValue && prev.HasValue)
        {
            var buy = curr < 30 && prev < curr;
            var sell = curr > 70 && prev > curr;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetUltimateOscillatorRating(decimal? value)
    {
        if (value.HasValue)
        {
            var buy = value > 70;
            var sell = value < 30;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetBullBearPowerRating(WithPreviousValue<BBP> item, bool? uptrend, bool? downtrend)
    {
        var curr = item.Current;
        var prev = item.Previous;

        if (uptrend.HasValue && downtrend.HasValue && curr.BullPower.HasValue && curr.BearPower.HasValue && prev.BullPower.HasValue && prev.BearPower.HasValue)
        {
            var buy = uptrend.Value && curr.BearPower < 0 && curr.BullPower > prev.BearPower;
            var sell = downtrend.Value && curr.BullPower > 0 && curr.BullPower < prev.BullPower;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetWilliamsPercentRangeRating(WithPreviousValue<decimal?> item)
    {
        var curr = item.Current;
        var prev = item.Previous;

        if (curr.HasValue && prev.HasValue)
        {
            var buy = curr < -80 && curr > prev;
            var sell = curr > -20 && curr < prev;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetStochRsiRating(WithPreviousValue<StochasticRelativeStrengthValue> item, bool? uptrend, bool? downtrend)
    {
        var currK = item.Current.K;
        var currD = item.Current.D;
        var prevK = item.Previous.K;
        var prevD = item.Previous.D;

        if (downtrend.HasValue && uptrend.HasValue && currK.HasValue && currD.HasValue && prevK.HasValue && prevD.HasValue)
        {
            var buy = downtrend.Value && currK < 20 && currD < 20 && currK > currD && prevK < prevD;
            var sell = uptrend.Value && currK > 80 && currD > 80 && currK < currD && prevK > prevD;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetMacdRating(MacdValue item)
    {
        if (item.Macd.HasValue && item.Signal.HasValue)
        {
            var buy = item.Macd > item.Signal;
            var sell = item.Macd < item.Signal;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetMomentumRating(WithPreviousValue<decimal?> item)
    {
        var curr = item.Current;
        var prev = item.Previous;

        if (curr.HasValue && prev.HasValue)
        {
            var buy = curr > prev;
            var sell = curr < prev;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetAdxRating(WithPreviousValue<DMI> item)
    {
        var currAdx = item.Current.Adx;
        var currPlus = item.Current.Plus;
        var currMinus = item.Current.Minus;
        var prevPlus = item.Previous.Plus;
        var prevMinus = item.Previous.Minus;

        if (currAdx.HasValue && currPlus.HasValue && currMinus.HasValue && prevPlus.HasValue && prevMinus.HasValue)
        {
            var buy = currAdx > 20 && prevPlus < prevMinus && currPlus > currMinus;
            var sell = currAdx > 20 && prevPlus > prevMinus && currPlus < currMinus;
            return GetRating(buy, sell);
        }

        return null;
    }

    private static int? GetAwesomeOscilatorRating(IEnumerable<decimal?> window)
    {
        var list = window.ToList();

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

    public static bool TryGetTechnicalRatingsSummaryUp(this IEnumerable<Kline> source, out TechnicalRatingSummary result, TechnicalRatingAction target = TechnicalRatingAction.Buy, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(iterations, 1, nameof(iterations));

        result = TechnicalRatingSummary.Empty;

        // the last summary must not be in buy action already
        var last = source.TechnicalRatingsSummary().Last();
        if (last.Summary.Action >= target)
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

        // use the last kline as a template for the future one
        var kline = source.Last();
        kline = kline with
        {
            OpenPrice = kline.ClosePrice,
            HighPrice = kline.ClosePrice,
            LowPrice = kline.ClosePrice
        };

        // perform binary search
        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateKline = kline with
            {
                ClosePrice = candidatePrice,
                HighPrice = Math.Max(kline.HighPrice, candidatePrice),
                LowPrice = Math.Min(kline.LowPrice, candidatePrice)
            };
            var candidateSummary = source.Append(candidateKline).TechnicalRatingsSummary().Last();

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

    public static bool TryGetTechnicalRatingsSummaryDown(this IEnumerable<Kline> source, out TechnicalRatingSummary result, TechnicalRatingAction target = TechnicalRatingAction.Sell, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(iterations, 1, nameof(iterations));

        result = TechnicalRatingSummary.Empty;

        // the last summary must not be in sell action already
        var last = source.TechnicalRatingsSummary().Last();
        if (last.Summary.Action <= target)
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

        // use the last kline as a template for the future one
        var kline = source.Last();
        kline = kline with
        {
            OpenPrice = kline.ClosePrice,
            HighPrice = kline.ClosePrice,
            LowPrice = kline.ClosePrice
        };

        // perform binary search
        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateKline = kline with
            {
                ClosePrice = candidatePrice,
                HighPrice = Math.Max(kline.HighPrice, candidatePrice),
                LowPrice = Math.Min(kline.LowPrice, candidatePrice)
            };
            var candidateSummary = source.Append(candidateKline).TechnicalRatingsSummary().Last();

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