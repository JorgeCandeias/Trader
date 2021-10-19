using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IKlinePublisher
    {
        void Publish(Kline kline);
    }
}