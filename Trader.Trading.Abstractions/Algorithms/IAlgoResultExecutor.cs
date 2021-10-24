using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoResultExecutor<in TAlgoResult>
        where TAlgoResult : IAlgoResult
    {
        Task ExecuteAsync(TAlgoResult result);
    }
}