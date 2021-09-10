using Orleans.Runtime;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Watchdog
{
    public interface IWatchdogGrainExtension : IGrainExtension
    {
        Task PingAsync();
    }
}