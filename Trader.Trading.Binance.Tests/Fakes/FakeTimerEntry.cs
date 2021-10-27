using Orleans;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Tests.Fakes
{
    public sealed class FakeTimerEntry : IDisposable
    {
        public FakeTimerEntry(Grain grain, Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period, Action<FakeTimerEntry> onDispose)
        {
            Grain = grain;
            AsyncCallback = asyncCallback;
            State = state;
            DueTime = dueTime;
            Period = period;

            _onDispose = onDispose;
        }

        private Action<FakeTimerEntry>? _onDispose;

        public Grain Grain { get; }
        public Func<object, Task> AsyncCallback { get; }
        public object State { get; }
        public TimeSpan DueTime { get; }
        public TimeSpan Period { get; }

        public void Dispose()
        {
            Interlocked.Exchange(ref _onDispose, null)?.Invoke(this);
            GC.SuppressFinalize(this);
        }
    }
}