using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms.Context;

public class SymbolSavingsBalances
{
    public SavingsBalance BaseAsset { get; set; } = SavingsBalance.Empty;

    public SavingsBalance QuoteAsset { get; set; } = SavingsBalance.Empty;
}