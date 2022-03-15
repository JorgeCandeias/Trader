namespace Outcompute.Trader.Trading.Indicators;

public interface IIndicator<in TSource, out TResult> : IReadOnlyList<TResult>
{
    /// <summary>
    /// Adds a new source value to the indicator.
    /// </summary>
    void Add(TSource value);

    /// <summary>
    /// Updates the last source value in the indicator.
    /// </summary>
    void Update(TSource value);

    /// <summary>
    /// Resets the indicator by clearing all data.
    /// </summary>
    void Reset();
}