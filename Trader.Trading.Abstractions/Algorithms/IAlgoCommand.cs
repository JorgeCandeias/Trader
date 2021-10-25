using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoCommand
    {
        Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default);
    }
}