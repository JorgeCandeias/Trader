using Orleans;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    internal interface ISwapGrain : IGrainWithGuidKey
    {
        Task PingAsync();
    }
}