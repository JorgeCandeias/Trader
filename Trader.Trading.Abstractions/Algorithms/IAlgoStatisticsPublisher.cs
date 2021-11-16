using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms;

public interface IAlgoStatisticsPublisher
{
    Task PublishAsync(PositionDetails significant, MiniTicker ticker, CancellationToken cancellationToken = default);
}