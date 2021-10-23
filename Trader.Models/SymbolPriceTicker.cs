using Orleans.Concurrency;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record SymbolPriceTicker(
        string Symbol,
        decimal Price)
    {
        public static SymbolPriceTicker Empty { get; } = new SymbolPriceTicker(string.Empty, 0);
    }
}