using Outcompute.Trader.Trading.Algorithms.Context;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface IAlgoCommand
    {
        ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default);
    }
}