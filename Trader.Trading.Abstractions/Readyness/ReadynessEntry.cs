using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Readyness
{
    internal class ReadynessEntry : IReadynessEntry
    {
        private readonly Func<IServiceProvider, CancellationToken, ValueTask<bool>> _action;

        public ReadynessEntry(Func<IServiceProvider, CancellationToken, ValueTask<bool>> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public ValueTask<bool> IsReadyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
        {
            return _action(provider, cancellationToken);
        }
    }
}