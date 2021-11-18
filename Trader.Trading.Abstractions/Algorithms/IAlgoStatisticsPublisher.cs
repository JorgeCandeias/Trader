using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Positions;

namespace Outcompute.Trader.Trading.Algorithms;

public interface IAlgoStatisticsPublisher
{
    Task PublishAsync(AutoPosition significant, MiniTicker ticker, CancellationToken cancellationToken = default);
}