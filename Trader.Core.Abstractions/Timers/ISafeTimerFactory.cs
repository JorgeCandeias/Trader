using System;
using System.Threading.Tasks;

namespace Trader.Core.Timers
{
    public interface ISafeTimerFactory
    {
        ISafeTimer Create(Func<IDisposable, Task> callback, TimeSpan dueTime, TimeSpan period);
    }
}