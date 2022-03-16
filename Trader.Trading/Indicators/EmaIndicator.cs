﻿namespace Outcompute.Trader.Trading.Indicators;

public class EmaIndicator : IndicatorBase<decimal?, decimal?>
{
    public EmaIndicator(int periods = 10)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        Alpha = 2M / (periods + 1M);
    }

    public int Periods { get; }
    public decimal Alpha { get; }

    protected override decimal? Calculate(int index)
    {
        // skip until the indicator is seeded
        if (index < Periods - 1)
        {
            return null;
        }

        var ema = Result[^2];

        // start from the sma to avoid spikes
        if (!ema.HasValue)
        {
            var sum = 0M;
            var count = 0;

            for (var i = 0; i < Periods; i++)
            {
                var value = Source[i];
                if (value.HasValue)
                {
                    sum += value.Value;
                    count++;
                }
            }

            if (count > 0)
            {
                return sum / count;
            }
            else
            {
                return null;
            }
        }

        // calculate the next ema
        var next = Source[^1];
        if (next.HasValue)
        {
            return (Alpha * next) + (1 - Alpha) * ema;
        }
        else
        {
            return next;
        }
    }
}

public static class EmaIndicatorEnumerableExtensions
{
    public static IEnumerable<decimal?> ExponentialMovingAverage<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 10)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var indicator = new EmaIndicator(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> ExponentialMovingAverage(this IEnumerable<Kline> source, int periods)
    {
        return source.ExponentialMovingAverage(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> ExponentialMovingAverage(this IEnumerable<decimal?> source, int periods)
    {
        return source.ExponentialMovingAverage(x => x, periods);
    }
}