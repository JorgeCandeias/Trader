using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.InMemory.UserData
{
    internal class InMemoryUserDataStreamSender : IInMemoryUserDataStreamSender
    {
        private readonly ConcurrentDictionary<Func<UserDataStreamMessage, CancellationToken, Task>, bool> _actions = new();

        public IDisposable Register(Func<UserDataStreamMessage, CancellationToken, Task> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            _actions[action] = true;

            return new DisposableAction(() => _actions.TryRemove(action, out _));
        }

        public async Task SendAsync(UserDataStreamMessage message, CancellationToken cancellationToken = default)
        {
            foreach (var action in _actions.Keys)
            {
                await action(message, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}