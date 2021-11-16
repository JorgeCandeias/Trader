using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;

namespace Outcompute.Trader.Trading.Algorithms;

/// <summary>
/// Base interface for algos that do not follow the suggested lifecycle.
/// Consider implementing the Algo class for less effort.
/// </summary>
public interface IAlgo
{
    IAlgoContext Context { get; }

    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);

    ValueTask<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default);
}