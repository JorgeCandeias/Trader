using Orleans.Concurrency;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models;

[Immutable]
public record SwapPoolLiquidity(
    long PoolId,
    string PoolName,
    DateTime UpdateTime,
    ImmutableDictionary<string, decimal> Liquidity,
    decimal ShareAmount,
    decimal SharePercentage,
    ImmutableDictionary<string, decimal> AssetShare);