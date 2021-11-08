using Orleans;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData
{
    internal interface IBinanceUserDataReadynessGrain : IGrainWithGuidKey
    {
        Task<bool> IsReadyAsync();
    }
}