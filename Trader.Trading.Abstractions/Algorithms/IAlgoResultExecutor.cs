using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoResultExecutor<in TAlgoResult>
        where TAlgoResult : notnull, IAlgoResult
    {
        Task ExecuteAsync(IAlgoContext context, TAlgoResult result, CancellationToken cancellationToken = default);
    }

    public interface IAlgoResultExecutor<in TAlgoResult, TResult>
        where TAlgoResult : notnull, IAlgoResult
    {
        Task<TResult> ExecuteAsync(IAlgoContext context, TAlgoResult result, CancellationToken cancellationToken = default);
    }
}