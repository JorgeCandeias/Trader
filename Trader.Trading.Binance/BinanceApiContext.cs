namespace Outcompute.Trader.Trading.Binance;

internal static class BinanceApiContext
{
    private static readonly AsyncLocal<bool> _skipSigning = new();

    public static bool SkipSigning
    {
        get => _skipSigning.Value;
        set => _skipSigning.Value = value;
    }
}