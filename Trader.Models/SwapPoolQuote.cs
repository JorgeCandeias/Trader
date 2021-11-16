namespace Outcompute.Trader.Models;

public record SwapPoolQuote(
    string QuoteAsset,
    string BaseAsset,
    decimal QuoteQuantity,
    decimal BaseQuantity,
    decimal Price,
    decimal Slippage,
    decimal Fee);