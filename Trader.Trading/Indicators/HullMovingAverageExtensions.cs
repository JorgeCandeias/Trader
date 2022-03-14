﻿using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public static class HullMovingAverageExtensions
{
    public static IEnumerable<decimal?> HullMovingAverage(this IEnumerable<Kline> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        return source.HullMovingAverage(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> HullMovingAverage<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        return source.Select(selector).HullMovingAverage(periods);
    }

    public static IEnumerable<decimal?> HullMovingAverage(this IEnumerable<decimal?> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        var wma1 = source.WeightedMovingAverage(periods / 2);
        var wma2 = source.WeightedMovingAverage(periods);

        return wma1.Zip(wma2).Select(x => (2 * x.First) - x.Second).WeightedMovingAverage((int)Math.Floor(Math.Sqrt(periods)));
    }

    public static bool TryGetHullMovingAverageVelocityUp(this IEnumerable<Kline> source, out decimal price, int periods = 9, decimal velocity = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 2, nameof(periods));
        Guard.IsGreaterThanOrEqualTo(iterations, 1, nameof(iterations));

        return source.Select(x => (decimal?)x.ClosePrice).TryGetHullMovingaverageVelocityUp(out price, periods, velocity, iterations);
    }

    public static bool TryGetHullMovingaverageVelocityUp(this IEnumerable<decimal?> source, out decimal price, int periods = 9, decimal velocity = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 2, nameof(periods));
        Guard.IsGreaterThanOrEqualTo(iterations, 1, nameof(iterations));

        price = 0M;

        // the last value must be in downtrend
        var last = source.HullMovingAverage(periods).Change().Last();
        if (!last.HasValue || last.Value >= velocity)
        {
            return false;
        }

        // define the upper search range
        var high = source.Max() * 2M;
        if (!high.HasValue)
        {
            return false;
        }

        // define the lower search range
        var low = source.Min() / 2M;
        if (!low.HasValue)
        {
            return false;
        }

        // perform binary search
        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateVelocity = source.Append(candidatePrice).HullMovingAverage(periods).Change().Last();

            // adjust ranges to search for a better candidate
            if (candidateVelocity.HasValue)
            {
                if (candidateVelocity.Value > velocity)
                {
                    price = candidatePrice.Value;
                    high = candidatePrice;
                }
                else if (candidateVelocity.Value < velocity)
                {
                    low = candidatePrice;
                }
                else
                {
                    price = candidatePrice.Value;
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        return price != 0M;
    }

    public static bool TryGetHullMovingAverageVelocityDown(this IEnumerable<Kline> source, out decimal price, int periods = 9, decimal velocity = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 2, nameof(periods));
        Guard.IsGreaterThanOrEqualTo(iterations, 1, nameof(iterations));

        return source.Select(x => (decimal?)x.ClosePrice).TryGetHullMovingaverageVelocityDown(out price, periods, velocity, iterations);
    }

    public static bool TryGetHullMovingaverageVelocityDown(this IEnumerable<decimal?> source, out decimal price, int periods = 9, decimal velocity = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 2, nameof(periods));
        Guard.IsGreaterThanOrEqualTo(iterations, 1, nameof(iterations));

        price = 0M;

        // the last value must be in uptrend
        var last = source.HullMovingAverage(periods).Change().Last();
        if (!last.HasValue || last.Value >= velocity)
        {
            return false;
        }

        // define the upper search range
        var high = source.Max() * 2M;
        if (!high.HasValue)
        {
            return false;
        }

        // define the lower search range
        var low = source.Min() / 2M;
        if (!low.HasValue)
        {
            return false;
        }

        // perform binary search
        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateVelocity = source.Append(candidatePrice).HullMovingAverage(periods).Change().Last();

            // adjust ranges to search for a better candidate
            if (candidateVelocity.HasValue)
            {
                if (candidateVelocity.Value > velocity)
                {
                    high = candidatePrice;
                }
                else if (candidateVelocity.Value < velocity)
                {
                    price = candidatePrice.Value;
                    low = candidatePrice;
                }
                else
                {
                    price = candidatePrice.Value;
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        return price != 0M;
    }
}