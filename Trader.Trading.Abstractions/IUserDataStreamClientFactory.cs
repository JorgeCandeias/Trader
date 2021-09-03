namespace Outcompute.Trader.Trading
{
    public interface IUserDataStreamClientFactory
    {
        IUserDataStreamClient Create(string listenKey);
    }
}