namespace Outcompute.Trader.Trading.Algorithms.Context
{
    internal class AlgoContextLocal : IAlgoContextLocal
    {
        private IAlgoContext? _context;

        public IAlgoContext Context
        {
            get
            {
                return _context ?? throw new InvalidOperationException();
            }
            set
            {
                _context = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
    }
}