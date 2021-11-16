using Orleans.Concurrency;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models;

[Immutable]
public record SwapPoolConfiguration(
    long PoolId,
    string PoolName,
    DateTime UpdateTime,
    SwapPoolConfigurationLiquidity Liquidity,
    ImmutableDictionary<string, SwapPoolConfigurationAsset> Assets);

[Immutable]
public record SwapPoolConfigurationLiquidity(
    decimal MinShareRedemption,
    decimal SlippageTolerance);

[Immutable]
public record SwapPoolConfigurationAsset(
    decimal MinAdd,
    decimal MaxAdd,
    decimal MinSwap,
    decimal MaxSwap);