using Orleans.Concurrency;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models;

/// <summary>
/// Swap Pool balances for an asset.
/// </summary>
/// <param name="Asset">The asset the swap pool balance belongs to.</param>
/// <param name="Total">The total asset amount in swap pools.</param>
/// <param name="Details">Swap pool details that compose the total balance.</param>
[Immutable]
public record SwapPoolAssetBalance(string Asset, decimal Total, ImmutableList<SwapPoolAssetBalanceDetail> Details)
{
    /// <summary>
    /// Gets a zero balance instance with no asset.
    /// </summary>
    public static SwapPoolAssetBalance Empty { get; } = new SwapPoolAssetBalance(string.Empty, 0m, ImmutableList<SwapPoolAssetBalanceDetail>.Empty);

    /// <summary>
    /// Gets a zero balance instance with the specified asset.
    /// </summary>
    public static SwapPoolAssetBalance Zero(string asset) => Empty with { Asset = asset };
}

[Immutable]
public record SwapPoolAssetBalanceDetail(string PoolName, decimal Amount);