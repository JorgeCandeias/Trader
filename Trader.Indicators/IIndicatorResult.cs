namespace Trader.Indicators;

public interface IIndicatorResult<out TResult> : IReadOnlyList<TResult>, IDisposable
{
    /// <summary>
    /// Registers a callback that will be raised when a result item changes.
    /// The callback is executed synchronously immediately after the new value is available.
    /// </summary>
    IDisposable RegisterChangeCallback(Action<int> action);
}