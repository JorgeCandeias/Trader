using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms.Context;

public class SymbolSpotBalances
{
    public Balance BaseAsset { get; set; } = Balance.Empty;

    public Balance QuoteAsset { get; set; } = Balance.Empty;
}