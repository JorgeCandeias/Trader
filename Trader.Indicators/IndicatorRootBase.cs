namespace Outcompute.Trader.Indicators;

public class IndicatorRootBase<TResult> : IndicatorResult<TResult>
{
    private readonly List<TResult> _result = new();

    public override TResult this[int index] => _result[index];

    public override int Count => _result.Count;

    protected void Set(int index, TResult value)
    {
        Guard.IsGreaterThanOrEqualTo(index, 0, nameof(index));

        // if the value is in range then update it
        if (index < _result.Count)
        {
            _result[index] = value;
            RaiseCallback(index);
            return;
        }

        // add all gaps up to the new value
        for (var i = _result.Count; i < index; i++)
        {
            _result.Add(default!);
            RaiseCallback(index);
        }

        // add the new value at the end
        _result.Add(value);
        RaiseCallback(index);
    }

    public override IEnumerator<TResult> GetEnumerator() => _result.GetEnumerator();
}