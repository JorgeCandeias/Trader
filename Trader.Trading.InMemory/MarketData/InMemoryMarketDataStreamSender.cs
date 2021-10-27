using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.InMemory.MarketData;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.InMemory
{
    public class InMemoryMarketDataStreamSender : IInMemoryMarketDataStreamSender
    {
        public IDisposable Register(Func<MarketDataStreamMessage, CancellationToken, Task> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            _actions[action] = true;

            return new DisposableAction(() => _actions.TryRemove(action, out _));
        }

        private readonly ConcurrentDictionary<Func<MarketDataStreamMessage, CancellationToken, Task>, bool> _actions = new();

        public async Task SendAsync(MarketDataStreamMessage message, CancellationToken cancellationToken = default)
        {
            foreach (var action in _actions.Keys)
            {
                await action(message, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}