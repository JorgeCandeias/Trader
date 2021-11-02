using System;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models
{
    public record SwapPoolLiquidity(
        long PoolId,
        string PoolName,
        DateTime UpdatedTime,
        ImmutableDictionary<string, decimal> Liquidity,
        decimal ShareAmount,
        decimal SharePercentage,
        ImmutableDictionary<string, decimal> AssetShare);
}