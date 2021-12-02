using Outcompute.Trader.Trading.Iterators;

namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// An iterator that calculates the RSI over the specified source.
/// </summary>
internal sealed class RsiIterator : Iterator<decimal>
{
    private readonly IEnumerable<decimal> _source;
    private readonly IEnumerator<decimal> _avgGain;
    private readonly IEnumerator<decimal> _avgLoss;
    private readonly int _periods;

    public RsiIterator(IEnumerable<decimal> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        _source = source;
        _periods = periods;

        _avgGain = _source.Gain().Rma(periods).GetEnumerator();
        _avgLoss = _source.AbsLoss().Rma(periods).GetEnumerator();

        _state = 1;
    }

    public override bool MoveNext()
    {
        switch (_state)
        {
            // rsi - single step
            case 1:
                if (_avgGain.MoveNext() && _avgLoss.MoveNext())
                {
                    if (_avgLoss.Current is 0M)
                    {
                        if (_avgGain.Current is 0M)
                        {
                            _current = 50M;
                        }
                        else
                        {
                            _current = 100m;
                        }
                    }
                    else
                    {
                        var rs = _avgGain.Current / _avgLoss.Current;

                        _current = 100m - (100m / (1m + rs));
                    }

                    return true;
                }
                else
                {
                    _state = -1;

                    return false;
                }
        }

        return false;
    }

    protected override Iterator<decimal> Clone() => new RsiIterator(_source, _periods);

    protected override void Dispose(bool disposing)
    {
        _state = -1;
        _avgGain.Dispose();
        _avgLoss.Dispose();

        base.Dispose(disposing);
    }
}