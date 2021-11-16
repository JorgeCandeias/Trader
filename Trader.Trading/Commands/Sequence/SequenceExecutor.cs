using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.Sequence
{
    internal class SequenceExecutor : IAlgoCommandExecutor<SequenceCommand>
    {
        public async ValueTask ExecuteAsync(IAlgoContext context, SequenceCommand command, CancellationToken cancellationToken = default)
        {
            foreach (var item in command.Commands)
            {
                await item
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}