namespace Outcompute.Trader.Core;

/// <summary>
/// Provides helper math functions over <see cref="ReadOnlySpan{T}"/>.
/// </summary>
public static class MathSpan
{
    private static void EnsureValuesNotEmpty<T>(ReadOnlySpan<T> values)
    {
        if (values.Length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(values), $"Parameter '{nameof(values)}' is empty");
        }
    }

    public static int Max(ReadOnlySpan<int> values)
    {
        EnsureValuesNotEmpty(values);

        var max = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            var value = values[i];
            if (value > max)
            {
                max = value;
            }
        }
        return max;
    }

    public static decimal Max(ReadOnlySpan<decimal> values)
    {
        EnsureValuesNotEmpty(values);

        var max = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            var value = values[i];
            if (value > max)
            {
                max = value;
            }
        }
        return max;
    }

    public static T Max<T>(ReadOnlySpan<T> values)
        where T : IComparable<T>
    {
        EnsureValuesNotEmpty(values);

        var max = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            var value = values[i];
            if (Comparer<T>.Default.Compare(value, max) > 0)
            {
                max = value;
            }
        }
        return max;
    }

    public static int Min(ReadOnlySpan<int> values)
    {
        EnsureValuesNotEmpty(values);

        var min = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            var value = values[i];
            if (value < min)
            {
                min = value;
            }
        }
        return min;
    }

    public static decimal Min(ReadOnlySpan<decimal> values)
    {
        EnsureValuesNotEmpty(values);

        var min = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            var value = values[i];
            if (value < min)
            {
                min = value;
            }
        }
        return min;
    }

    public static T Min<T>(ReadOnlySpan<T> values)
        where T : IComparable<T>
    {
        EnsureValuesNotEmpty(values);

        var min = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            var value = values[i];
            if (Comparer<T>.Default.Compare(value, min) < 0)
            {
                min = value;
            }
        }
        return min;
    }
}