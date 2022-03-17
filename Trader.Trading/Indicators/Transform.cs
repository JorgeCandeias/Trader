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

public static partial class Indicator
{
    public static Transform<TSource, TResult> Transform<TSource, TResult>(Func<TSource, TResult> transform) => new(transform);

    public static Transform<decimal?, decimal?> Transform(Func<decimal?, decimal?> transform) => Transform<decimal?, decimal?>(transform);

    public static Transform<TSource, TResult> Transform<TSource, TResult>(IIndicatorResult<TSource> source, Func<TSource, TResult> transform) => new(source, transform);

    public static Transform<decimal?, decimal?> Transform(IIndicatorResult<decimal?> source, Func<decimal?, decimal?> transform) => Transform<decimal?, decimal?>(source, transform);
}