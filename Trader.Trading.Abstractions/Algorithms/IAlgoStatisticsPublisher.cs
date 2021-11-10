using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoStatisticsPublisher
    {
        Task PublishAsync(PositionDetails significant, MiniTicker ticker, CancellationToken cancellationToken = default);
    }
}