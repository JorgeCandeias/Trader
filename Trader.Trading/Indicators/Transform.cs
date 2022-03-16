namespace Outcompute.Trader.Trading.Indicators;

public class Transform<TSource, TResult> : IndicatorBase<TSource, TResult>
{
    private readonly Func<TSource, TResult> _transform;

    public Transform(Func<TSource, TResult> transform)
    {
        Guard.IsNotNull(transform, nameof(transform));

        _transform = transform;
    }

    public Transform(IIndicatorResult<TSource> source, Func<TSource, TResult> transform)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(transform, nameof(transform));

        _transform = transform;

        LinkFrom(source);
    }

    protected override TResult Calculate(int index)
    {
        return _transform(Source[index]);
    }
}