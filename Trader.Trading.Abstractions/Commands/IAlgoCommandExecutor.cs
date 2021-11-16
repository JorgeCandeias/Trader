using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands;

public interface IAlgoCommandExecutor<in TAlgoCommand>
    where TAlgoCommand : notnull, IAlgoCommand
{
    ValueTask ExecuteAsync(IAlgoContext context, TAlgoCommand command, CancellationToken cancellationToken = default);
}

public interface IAlgoCommandExecutor<in TAlgoCommand, TResult>
    where TAlgoCommand : notnull, IAlgoCommand
{
    ValueTask<TResult> ExecuteAsync(IAlgoContext context, TAlgoCommand command, CancellationToken cancellationToken = default);
}