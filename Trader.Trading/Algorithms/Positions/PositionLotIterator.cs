using Outcompute.Trader.Trading.Iterators;

namespace Outcompute.Trader.Trading.Algorithms.Positions
{
    internal class PositionLotIterator : Iterator<PositionLot>
    {
        private readonly IEnumerable<Position> _source;
        private readonly decimal _size;

        private readonly IEnumerator<Position> _enumerator;

        public PositionLotIterator(IEnumerable<Position> source, decimal size)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _size = size > 0 ? size : throw new ArgumentOutOfRangeException(nameof(size));

            _enumerator = _source.GetEnumerator();
            _state = 1;
        }

        private decimal _remaining;
        private decimal _quantity;
        private decimal _notional;
        private DateTime _time;

        public override bool MoveNext()
        {
            switch (_state)
            {
                case 1:
                    if (TryFill())
                    {
                        _current = new PositionLot(_quantity, _notional / _quantity, _time);
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

        private bool TryFill()
        {
            _quantity = 0;
            _notional = 0;

            while (_quantity < _size)
            {
                while (_remaining == 0)
                {
                    if (!TryMoveNext())
                    {
                        return false;
                    }
                }

                var required = _size - _quantity;
                var taken = Math.Min(Math.Min(_remaining, _size), required);

                _quantity += taken;
                _remaining -= taken;
                _notional += taken * _enumerator.Current.Price;
            }

            return true;
        }

        private bool TryMoveNext()
        {
            if (!_enumerator.MoveNext())
            {
                return false;
            }

            var current = _enumerator.Current;
            _remaining = current.Quantity;
            _time = current.Time;

            if (_remaining < 0)
            {
                ThrowHelper.ThrowInvalidOperationException($"Cannot enumerate negative position quantity of {_remaining}");
            }

            return true;
        }

        protected override Iterator<PositionLot> Clone() => new PositionLotIterator(_source, _size);
    }
}