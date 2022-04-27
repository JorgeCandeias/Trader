namespace Outcompute.Trader.Indicators;

public class Transform<TSource, TResult> : IndicatorBase<TSource, TResult>
{
    private readonly Func<TSource, TResult> _transform;

    public Transform(IndicatorResult<TSource> source, Func<TSource, TResult> transform)
        : base(source, false)
    {
        Guard.IsNotNull(transform, nameof(transform));

        _transform = transform;

        Ready();
    }

    protected override TResult Calculate(int index)
    {
        return _transform(Source[index]);
    }
}

public static partial class Indicator
{
    public static Transform<TSource, TResult> Transform<TSource, TResult>(this IndicatorResult<TSource> source, Func<TSource, TResult> transform)
        => new(source, transform);
}