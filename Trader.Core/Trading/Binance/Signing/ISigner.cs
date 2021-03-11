namespace Trader.Core.Trading.Binance.Signing
{
    internal interface ISigner
    {
        string Sign(string value);
    }
}