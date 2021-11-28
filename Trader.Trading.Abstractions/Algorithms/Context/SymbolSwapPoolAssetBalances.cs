using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms.Context;

/// <summary>
/// Organizes information for swap pool balances of the assets of a symbol.
/// </summary>
public class SymbolSwapPoolAssetBalances
{
    /// <summary>
    /// The swap pool balance for the base asset of the symbol.
    /// </summary>
    public SwapPoolAssetBalance BaseAsset { get; set; } = SwapPoolAssetBalance.Empty;

    /// <summary>
    /// Swap pool balance for the quote asset of the symbol.
    /// </summary>
    public SwapPoolAssetBalance QuoteAsset { get; set; } = SwapPoolAssetBalance.Empty;
}