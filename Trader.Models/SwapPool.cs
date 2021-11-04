using Orleans.Concurrency;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record SwapPool(
        long PoolId,
        string PoolName,
        ImmutableHashSet<string> Assets);
}