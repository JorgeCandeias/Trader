namespace Outcompute.Trader.Models;

public record SwapPoolLiquidityAddPreview(
    string QuoteAsset,
    string BaseAsset,
    decimal QuoteAmount,
    decimal BaseAmount,
    decimal Price,
    decimal Share,
    decimal Slippage,
    decimal Fee);