using Orleans.Concurrency;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    [Immutable]
    public record AlgoInfo(
        string Name,
        string Type,
        bool Enabled,
        TimeSpan MaxExecutionTime,
        TimeSpan TickDelay,
        bool TickEnabled);
}