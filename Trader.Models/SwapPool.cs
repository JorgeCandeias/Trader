using System.Collections.Immutable;

namespace Outcompute.Trader.Models
{
    public record SwapPool(
        int PoolId,
        int PoolName,
        ImmutableHashSet<string> Assets);
}