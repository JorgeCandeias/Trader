using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Models;

[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "N/A")]
public enum SwapPoolLiquidityType
{
    None,
    Single,
    Combination
}