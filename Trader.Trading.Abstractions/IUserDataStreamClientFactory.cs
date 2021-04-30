namespace Trader.Trading
{
    public interface IUserDataStreamClientFactory
    {
        IUserDataStreamClient Create(string listenKey);
    }
}