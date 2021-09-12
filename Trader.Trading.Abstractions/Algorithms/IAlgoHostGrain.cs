using Orleans;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// Hosts symbol based algorithms and pull necessary data so the algo code itself can focus on the math.
    /// </summary>
    public interface IAlgoHostGrain : IGrainWithStringKey
    {
        Task PingAsync();
    }
}