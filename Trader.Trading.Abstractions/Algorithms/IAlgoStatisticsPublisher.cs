using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms;

public interface IAlgoStatisticsPublisher
{
    Task PublishAsync(AutoPosition significant, MiniTicker ticker, CancellationToken cancellationToken = default);
}