using System;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models
{
    public record SwapPoolConfiguration(
        long PoolId,
        string PoolName,
        DateTime UpdateTime,
        SwapPoolConfigurationLiquidity Liquidity,
        ImmutableDictionary<string, SwapPoolConfigurationAsset> Assets);

    public record SwapPoolConfigurationLiquidity(
        decimal MinShareRedemption,
        decimal SlippageTolerance);

    public record SwapPoolConfigurationAsset(
        decimal MinAdd,
        decimal MaxAdd,
        decimal MinSwap,
        decimal MaxSwap);
}