namespace Outcompute.Trader.Trading.Indicators;

public class TransformIndicator<TSource, TResult> : IndicatorBase<TSource, TResult>
{
    private readonly Func<IList<TSource>, TResult> _transform;

    public TransformIndicator(Func<IList<TSource>, TResult> transform)
    {
        Guard.IsNotNull(transform, nameof(transform));

        _transform = transform;
    }

    protected override TResult Calculate(int index)
    {
        return _transform(Source);
    }
}