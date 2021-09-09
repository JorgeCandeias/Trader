namespace Outcompute.Trader.Trading.Binance.Signing
{
    internal interface ISigner
    {
        string Sign(string value);
    }
}