using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Readyness
{
    internal class ReadynessEntry : IReadynessEntry
    {
        private readonly Func<IServiceProvider, CancellationToken, Task<bool>> _action;

        public ReadynessEntry(Func<IServiceProvider, CancellationToken, Task<bool>> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public Task<bool> IsReadyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
        {
            return _action(provider, cancellationToken);
        }
    }
}