using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.Many
{
    internal class ManyExecutor : IAlgoCommandExecutor<ManyCommand>
    {
        public async Task ExecuteAsync(IAlgoContext context, ManyCommand result, CancellationToken cancellationToken = default)
        {
            foreach (var item in result.Results)
            {
                await item
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}