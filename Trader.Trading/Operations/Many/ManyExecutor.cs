using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.Many
{
    internal class ManyExecutor : IAlgoResultExecutor<ManyAlgoResult>
    {
        public async Task ExecuteAsync(IAlgoContext context, ManyAlgoResult result, CancellationToken cancellationToken = default)
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