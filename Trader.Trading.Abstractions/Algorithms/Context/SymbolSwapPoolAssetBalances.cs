using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context;

public class SymbolSwapPoolAssetBalances
{
    public SwapPoolAssetBalance BaseAsset { get; set; } = SwapPoolAssetBalance.Empty;

    public SwapPoolAssetBalance QuoteAsset { get; set; } = SwapPoolAssetBalance.Empty;
}