namespace Outcompute.Trader.Trading.Indicators;

public enum KdjSide
{
    None,
    Up,
    Down
}

public enum KdjCross
{
    None,
    Up,
    Down
}

public record struct KdjValue
{
    public decimal? Price { get; init; }

    public decimal? K { get; init; }
    public decimal? D { get; init; }
    public decimal? J { get; init; }

    public KdjCross Cross { get; init; }

    public KdjSide Side
    {
        get
        {
            if (K > D && J > K) return KdjSide.Up;
            if (K < D && J < K) return KdjSide.Down;
            return KdjSide.None;
        }
    }

    public static KdjValue Empty => new();
}

public class Kdj : IndicatorBase<HLC, KdjValue>
{
    internal const int DefaultPeriods = 9;
    internal const int DefaultMa1 = 3;
    internal const int DefaultMa2 = 3;

    private readonly Highest _highest;
    private readonly Lowest _lowest;

    public Kdj(IndicatorResult<HLC> source, int periods = DefaultPeriods, int ma1 = DefaultMa1, int ma2 = DefaultMa2)
        : base(source, true)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));
        Guard.IsGreaterThanOrEqualTo(ma1, 1, nameof(ma1));
        Guard.IsGreaterThanOrEqualTo(ma2, 1, nameof(ma2));

        // keep useful series on-hand
        _highest = Indicator.Highest(source, periods, true);
        _lowest = Indicator.Lowest(source, periods, true);

        Periods = periods;
        Ma1 = ma1;
        Ma2 = ma2;

        Ready();
    }

    public int Periods { get; }
    public int Ma1 { get; }
    public int Ma2 { get; }

    protected override KdjValue Calculate(int index)
    {
        // take helper values
        var close = Source[index].Close;
        var max = _highest[index];
        var min = _lowest[index];

        // calculate the first output
        if (index < 1)
        {
            var diff = max - min;
            var rsv = diff == 0 ? 50 : (close - min) / diff * 100M;
            var a = rsv;
            var b = a;
            var e = 3M * a - 2M * b;

            return new KdjValue
            {
                Cross = KdjCross.None,
                Price = close,
                K = a,
                D = b,
                J = e
            };
        }
        else
        {
            var amp = max - min;
            var rsv = amp == 0 ? 0 : (close - min) / amp * 100M;
            var a = (rsv + (Ma1 - 1) * Result[index - 1].K) / Ma1;
            var b = (a + (Ma2 - 1) * Result[index - 1].D) / Ma2;
            var e = 3 * a - 2 * b;

            var cross = KdjCross.None;
            if (Result[index - 1].K < Result[index - 1].D && a > b)
            {
                cross = KdjCross.Up;
            }
            else if (Result[index - 1].K > Result[index - 1].D && a < b)
            {
                cross = KdjCross.Down;
            }

            return new KdjValue
            {
                Cross = cross,
                Price = close,
                K = a,
                D = b,
                J = e
            };
        }
    }
}

public static partial class Indicator
{
    public static Kdj Kdj(this IndicatorResult<HLC> source, int periods = Indicators.Kdj.DefaultPeriods, int ma1 = Indicators.Kdj.DefaultMa1, int ma2 = Indicators.Kdj.DefaultMa2)
        => new(source, periods, ma1, ma2);

    public static IEnumerable<KdjValue> ToKdj(this IEnumerable<HLC> source, int periods = Indicators.Kdj.DefaultPeriods, int ma1 = Indicators.Kdj.DefaultMa1, int ma2 = Indicators.Kdj.DefaultMa2)
        => source.Identity().Kdj(periods, ma1, ma2);

    public static IEnumerable<KdjValue> ToKdj(this IEnumerable<Kline> source, int periods = Indicators.Kdj.DefaultPeriods, int ma1 = Indicators.Kdj.DefaultMa1, int ma2 = Indicators.Kdj.DefaultMa2)
        => source.Select(x => new HLC(x.HighPrice, x.LowPrice, x.ClosePrice)).ToKdj();
}

public static class KdjEnumerableExtensions
{
    public static bool TryGetKdjForUpcross(this IEnumerable<Kline> source, Kline template, out KdjValue value, int periods = 9, int ma1 = 3, int ma2 = 3, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(template, nameof(template));

        value = KdjValue.Empty;

        // the last kdj must be in downtrend
        var last = source.ToKdj(periods, ma1, ma2).Last();
        if (last.Side != KdjSide.Down)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max(x => x.ClosePrice) * 2M;
        var low = source.Min(x => x.ClosePrice) / 2M;

        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            var candidateKline = template with
            {
                ClosePrice = candidatePrice,
                HighPrice = Math.Max(template.HighPrice, candidatePrice),
                LowPrice = Math.Min(template.LowPrice, candidatePrice)
            };

            // probe halfway between the range
            var candidateKdj = source.Append(candidateKline).ToKdj(periods, ma1, ma2).Last();

            // keep the best candidate so far
            if (candidateKdj.Side == KdjSide.Up)
            {
                value = candidateKdj;
            }

            // adjust ranges to search for a better candidate
            if (candidateKdj.Side == KdjSide.Up)
            {
                high = candidatePrice;
            }
            else if (candidateKdj.Side == KdjSide.Down)
            {
                low = candidatePrice;
            }
            else
            {
                value = candidateKdj;
                return true;
            }
        }

        return value != KdjValue.Empty;
    }

    public static bool TryGetKdjForDowncross(this IEnumerable<Kline> source, Kline template, out KdjValue value, int periods = 9, int ma1 = 3, int ma2 = 3, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(template, nameof(template));

        value = KdjValue.Empty;

        // the last kdj must be in uptrend
        var last = source.ToKdj(periods, ma1, ma2).Last();
        if (last.Side != KdjSide.Up)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max(x => x.ClosePrice) * 2M;
        var low = source.Min(x => x.ClosePrice) / 2M;

        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            var candidateKline = template with
            {
                ClosePrice = candidatePrice,
                HighPrice = Math.Max(template.HighPrice, candidatePrice),
                LowPrice = Math.Min(template.LowPrice, candidatePrice)
            };

            // probe halfway between the range
            var candidateKdj = source.Append(candidateKline).ToKdj(periods, ma1, ma2).Last();

            // keep the best candidate so far
            if (candidateKdj.Side == KdjSide.Down)
            {
                value = candidateKdj;
            }

            // adjust ranges to search for a better candidate
            if (candidateKdj.Side == KdjSide.Up)
            {
                high = candidatePrice;
            }
            else if (candidateKdj.Side == KdjSide.Down)
            {
                low = candidatePrice;
            }
            else
            {
                value = candidateKdj;
                return true;
            }
        }

        return value != KdjValue.Empty;
    }

    public static bool TryGetKdjForDivergenceUpcross(this IEnumerable<Kline> source, Kline template, out KdjValue value, decimal j = 0, int periods = 9, int ma1 = 3, int ma2 = 3, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(template, nameof(template));

        value = KdjValue.Empty;

        // the last kdj divergence must be negative
        var last = source.ToKdj(periods, ma1, ma2).Last();
        if (last.J >= j)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max(x => x.ClosePrice) * 2M;
        var low = source.Min(x => x.ClosePrice) / 2M;

        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            var candidateKline = template with
            {
                ClosePrice = candidatePrice,
                HighPrice = Math.Max(template.HighPrice, candidatePrice),
                LowPrice = Math.Min(template.LowPrice, candidatePrice)
            };

            // probe halfway between the range
            var candidateKdj = source.Append(candidateKline).ToKdj(periods, ma1, ma2).Last();

            // keep the best candidate so far
            if (candidateKdj.J >= j)
            {
                value = candidateKdj;
            }

            // adjust ranges to search for a better candidate
            if (candidateKdj.J > j)
            {
                high = candidatePrice;
            }
            else if (candidateKdj.J < j)
            {
                low = candidatePrice;
            }
            else
            {
                value = candidateKdj;
                return true;
            }
        }

        return value != KdjValue.Empty;
    }

    public static bool TryGetKdjForDivergenceDowncross(this IEnumerable<Kline> source, Kline template, out KdjValue value, int periods = 9, decimal j = 80, int ma1 = 3, int ma2 = 3, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(template, nameof(template));

        value = KdjValue.Empty;

        // the last kdj divergence must be extreme
        var last = source.ToKdj(periods, ma1, ma2).Last();
        if (last.J <= j)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max(x => x.ClosePrice) * 2M;
        var low = source.Min(x => x.ClosePrice) / 2M;

        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            var candidateKline = template with
            {
                ClosePrice = candidatePrice,
                HighPrice = Math.Max(template.HighPrice, candidatePrice),
                LowPrice = Math.Min(template.LowPrice, candidatePrice)
            };

            // probe halfway between the range
            var candidateKdj = source.Append(candidateKline).ToKdj(periods, ma1, ma2).Last();

            // keep the best candidate so far
            if (candidateKdj.J < j)
            {
                value = candidateKdj;
            }

            // adjust ranges to search for a better candidate
            if (candidateKdj.J > j)
            {
                high = candidatePrice;
            }
            else if (candidateKdj.J < j)
            {
                low = candidatePrice;
            }
            else
            {
                value = candidateKdj;
                return true;
            }
        }

        return value != KdjValue.Empty;
    }
}