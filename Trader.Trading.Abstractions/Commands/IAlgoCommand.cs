using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface IAlgoCommand
    {
        ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default);
    }
}