using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoCommandExecutor<in TAlgoResult>
        where TAlgoResult : notnull, IAlgoCommand
    {
        Task ExecuteAsync(IAlgoContext context, TAlgoResult result, CancellationToken cancellationToken = default);
    }

    public interface IAlgoResultExecutor<in TAlgoResult, TResult>
        where TAlgoResult : notnull, IAlgoCommand
    {
        Task<TResult> ExecuteAsync(IAlgoContext context, TAlgoResult result, CancellationToken cancellationToken = default);
    }
}