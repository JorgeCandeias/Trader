namespace System.Collections.Generic;

public enum PsarDirection
{
    None = 0,
    Long = 1,
    Short = 2
}

public record struct PsarValue
{
    public PsarDirection Direction { get; init; }
    public decimal Value { get; init; }
}

public static class ParabolicStopAndReverseExtensions
{
    public static IEnumerable<PsarValue> ParabolicStopAndReverse(this IEnumerable<Kline> source, decimal accelerationFactor = 0.02M, decimal accelerationStep = 0.02M, decimal accelerationMax = 0.2M)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(accelerationFactor, 0, nameof(accelerationFactor));
        Guard.IsGreaterThan(accelerationStep, 0, nameof(accelerationStep));
        Guard.IsGreaterThan(accelerationMax, 0, nameof(accelerationMax));

        var up = false;
        var sar = 0M;
        var ep = 0M;
        var result = 0M;
        var af = 0M;

        var enumerator = source.GetEnumerator();

        // get the first kline to keep it
        if (!enumerator.MoveNext()) yield break;
        var previous = enumerator.Current;
        yield return new PsarValue
        {
            Direction = PsarDirection.None,
            Value = previous.ClosePrice
        };

        // get the second kline to seed variables
        if (!enumerator.MoveNext()) yield break;
        var current = enumerator.Current;
        Setup(current, previous, ref up, ref ep, ref sar);
        yield return new PsarValue
        {
            Direction = PsarDirection.None,
            Value = sar
        };

        // enumerate the remaining values
        while (enumerator.MoveNext())
        {
            previous = current;
            current = enumerator.Current;

            if (up)
            {
                CalculateLongPosition(current, previous, ref sar, ref up, ref ep, ref result, ref af, accelerationFactor, accelerationStep, accelerationMax);
            }
            else
            {
                CalculateShortPosition(current, previous, ref sar, ref up, ref ep, ref result, ref af, accelerationFactor, accelerationStep, accelerationMax);
            }

            yield return new PsarValue
            {
                Direction = up ? PsarDirection.Long : PsarDirection.Short,
                Value = result
            };
        }

        static void Setup(Kline current, Kline previous, ref bool up, ref decimal ep, ref decimal sar)
        {
            up = current.ClosePrice >= previous.ClosePrice;
            if (up)
            {
                ep = Math.Min(current.HighPrice, previous.HighPrice);
                sar = previous.LowPrice;
            }
            else
            {
                ep = Math.Min(current.LowPrice, previous.LowPrice);
                sar = previous.HighPrice;
            }
        }

        static void CalculateLongPosition(Kline current, Kline previous, ref decimal sar, ref bool up, ref decimal ep, ref decimal result, ref decimal af, decimal afSeed, decimal afStep, decimal afMax)
        {
            // check if the long position fell to short
            if (current.LowPrice <= sar)
            {
                // flip the trend
                up = false;

                // reset the sar to the extreme price
                sar = ep;

                // raise the sar to the recent high range
                sar = Math.Max(sar, previous.HighPrice);
                sar = Math.Max(sar, current.HighPrice);

                // yield the exit sar
                result = sar;

                // reset the acceleration factor
                af = afSeed;

                // reset the extreme price
                ep = current.LowPrice;

                // calculate the new sar
                sar += af * (ep - sar);

                // raise the sar to the recent high range
                sar = Math.Max(sar, previous.HighPrice);
                sar = Math.Max(sar, current.HighPrice);
            }
            else
            {
                // yield the sar from the last step
                result = sar;

                // update the acceleration factor and the extreme price
                if (current.HighPrice > ep)
                {
                    ep = current.HighPrice;
                    af += afStep;
                    af = Math.Min(af, afMax);
                }

                // calculate the new sar
                sar += af * (ep - sar);

                // lower the sar to the recent low range
                sar = Math.Min(sar, previous.LowPrice);
                sar = Math.Min(sar, current.LowPrice);
            }
        }

        static void CalculateShortPosition(Kline current, Kline previous, ref decimal sar, ref bool up, ref decimal ep, ref decimal result, ref decimal af, decimal afSeed, decimal afStep, decimal afMax)
        {
            // check if the short position raised to long
            if (current.HighPrice >= sar)
            {
                // flip the trend
                up = true;

                // reset the sar to the extreme price
                sar = ep;

                // lower the sar to the recent low range
                sar = Math.Min(sar, previous.LowPrice);
                sar = Math.Min(sar, current.LowPrice);

                // yield the new sar
                result = sar;

                // reset the acceleration factor
                af = afSeed;

                // reset the extreme price
                ep = current.HighPrice;

                // calculate the new sar
                sar += af * (ep - sar);

                // lower the sar to the recent low range
                sar = Math.Min(sar, previous.LowPrice);
                sar = Math.Min(sar, current.LowPrice);
            }
            else
            {
                // yield the sar from the last step
                result = sar;

                // update the acceleration factor and the extreme price
                if (current.LowPrice < ep)
                {
                    ep = current.LowPrice;
                    af += afStep;
                    af = Math.Min(af, afMax);
                }

                // calculate the new sar
                sar += af * (ep - sar);

                // raise the sar to the recent high range
                sar = Math.Max(sar, previous.HighPrice);
                sar = Math.Max(sar, current.HighPrice);
            }
        }
    }
}