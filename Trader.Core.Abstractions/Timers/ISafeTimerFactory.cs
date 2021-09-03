﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Core.Timers
{
    public interface ISafeTimerFactory
    {
        ISafeTimer Create(Func<CancellationToken, Task> callback, TimeSpan dueTime, TimeSpan period, TimeSpan timeout);
    }
}