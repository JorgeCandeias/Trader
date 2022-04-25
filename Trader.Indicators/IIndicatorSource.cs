namespace Outcompute.Trader.Indicators;

public interface IIndicatorSource<in TSource>
{
    /// <summary>
    /// Adds a new source value to the end of the indicator.
    /// </summary>
    void Add(TSource value);

    /// <summary>
    /// Updates the source value in the specific position of the indicator.
    /// </summary>
    void Update(int index, TSource value);
}