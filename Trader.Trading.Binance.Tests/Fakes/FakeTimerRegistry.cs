using Orleans;
using Orleans.Timers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Tests.Fakes
{
    public class FakeTimerRegistry : ITimerRegistry
    {
        public FakeTimerRegistry()
        {
            _onDisposeDelegate = OnDispose;
        }

        private readonly ConcurrentDictionary<FakeTimerEntry, bool> _entries = new();

        public IDisposable RegisterTimer(Grain grain, Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var entry = new FakeTimerEntry(grain, asyncCallback, state, dueTime, period, _onDisposeDelegate);

            _entries[entry] = true;

            return entry;
        }

        private readonly Action<FakeTimerEntry> _onDisposeDelegate;

        private void OnDispose(FakeTimerEntry entry)
        {
            _entries.TryRemove(entry, out _);
        }

        public IEnumerable<FakeTimerEntry> Entries => _entries.Keys;
    }
}