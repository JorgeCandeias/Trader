namespace Outcompute.Trader.Indicators;

public class CompositeIndicator<TSource, TResult> : IndicatorResult<TResult>
{
    private readonly IndicatorResult<TResult> _result;

    protected CompositeIndicator(IndicatorResult<TSource> source, Func<IndicatorResult<TSource>, IndicatorResult<TResult>> compose)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(compose, nameof(compose));

        _result = compose(source);
    }

    public override TResult this[int index] => _result[index];

    public override int Count => _result.Count;

    public override IEnumerator<TResult> GetEnumerator() => _result.GetEnumerator();
}