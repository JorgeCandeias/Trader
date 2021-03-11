using System.Threading;
using System.Threading.Tasks;

namespace Trader.Core.Trading.Algorithms
{
    public interface ITradingAlgorithm
    {
        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);
    }
}