using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms.Context;

/// <summary>
/// Organizes spot balance information for a symbol.
/// </summary>
public class SymbolSpotBalances
{
    /// <summary>
    /// The current spot balance for the base asset of the symbol.
    /// </summary>
    public Balance BaseAsset { get; set; } = Balance.Empty;

    /// <summary>
    /// The current spot balance for the quote asset of the symbol.
    /// </summary>
    public Balance QuoteAsset { get; set; } = Balance.Empty;
}