using Orleans.Concurrency;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Providers;

[Immutable]
public record SwapPoolAssetBalance(string Asset, decimal Total, ImmutableList<SwapPoolAssetBalanceDetail> Details)
{
    public static SwapPoolAssetBalance Empty { get; } = new SwapPoolAssetBalance(string.Empty, 0m, ImmutableList<SwapPoolAssetBalanceDetail>.Empty);

    public static SwapPoolAssetBalance Zero(string asset) => Empty with { Asset = asset };
}

[Immutable]
public record SwapPoolAssetBalanceDetail(string PoolName, decimal Amount);