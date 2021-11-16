using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands;

public interface IAlgoCommand
{
    ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default);
}