using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms.Context;

/// <summary>
/// Organizes savings balance information for a symbol.
/// </summary>
public class SymbolSavingsBalances
{
    /// <summary>
    /// The savings balance for the base asset of the symbol.
    /// </summary>
    public SavingsBalance BaseAsset { get; set; } = SavingsBalance.Empty;

    /// <summary>
    /// The savings balance for the quote asset of the symbol.
    /// </summary>
    public SavingsBalance QuoteAsset { get; set; } = SavingsBalance.Empty;
}