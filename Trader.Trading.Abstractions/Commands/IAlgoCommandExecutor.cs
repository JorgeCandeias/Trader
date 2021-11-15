using Outcompute.Trader.Trading.Algorithms.Context;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
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
}