using System.Collections;

namespace Outcompute.Trader.Trading.Indicators;

public abstract class IndicatorBase<TSource, TResult> : IIndicator<TSource, TResult>
{
    protected IList<TSource> Source { get; } = new List<TSource>();

    protected IList<TResult> Result { get; } = new List<TResult>();

    public TResult this[int index] => Result[index];

    public int Count => Result.Count;

    public void AddRange(IEnumerable<TSource> values)
    {
        Guard.IsNotNull(values, nameof(values));

        foreach (var value in values)
        {
            Add(value);
        }
    }

    public void Add(TSource value)
    {
        Source.Add(value);
        Result.Add(Calculate(Source.Count - 1));
    }

    public void Update(TSource value)
    {
        Source[^1] = value;
        Result[^1] = Calculate(Source.Count - 1);
    }

    public void Reset()
    {
        Source.Clear();
        Result.Clear();
    }

    protected abstract TResult Calculate(int index);

    public IEnumerator<TResult> GetEnumerator() => Result.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Result.GetEnumerator();
}