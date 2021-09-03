using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Core.Timers
{
    internal class SafeTimerFactory : ISafeTimerFactory
    {
        private readonly IServiceProvider _provider;

        public SafeTimerFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ISafeTimer Create(Func<CancellationToken, Task> callback, TimeSpan dueTime, TimeSpan period, TimeSpan timeout)
        {
            return ActivatorUtilities.CreateInstance<SafeTimer>(_provider, callback, dueTime, period, timeout);
        }
    }
}