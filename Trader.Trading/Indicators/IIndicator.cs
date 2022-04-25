using Trader.Indicators;

namespace Outcompute.Trader.Trading.Indicators;

public interface IIndicatorResult<out TResult> : IReadOnlyList<TResult>, IDisposable
{
    /// <summary>
    /// Registers a callback that will be raised when a result item changes.
    /// The callback is executed synchronously immediately after the new value is available.
    /// </summary>
    IDisposable RegisterChangeCallback(Action<int> action);
}

public interface IIndicator<TSource, TResult> : IIndicatorSource<TSource>, IIndicatorResult<TResult>
{
}

public static class IIndicatorExtensions
{
    /// <summary>
    /// Adds a new source value to the indicator.
    /// </summary>
    public static void AddRange<TSource>(this IIndicatorSource<TSource> indicator, IEnumerable<TSource> values)
    {
        Guard.IsNotNull(indicator, nameof(indicator));
        Guard.IsNotNull(values, nameof(values));

        foreach (var value in values)
        {
            indicator.Add(value);
        }
    }
}