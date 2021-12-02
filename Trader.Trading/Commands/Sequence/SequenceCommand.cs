using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.Sequence;

public class SequenceCommand : IAlgoCommand
{
    public SequenceCommand(IEnumerable<IAlgoCommand> commands)
    {
        Guard.IsNotNull(commands, nameof(commands));

        Commands = commands;
    }

    public SequenceCommand(params IAlgoCommand[] results)
    {
        Guard.IsNotNull(results, nameof(results));

        Commands = results;
    }

    public IEnumerable<IAlgoCommand> Commands { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<SequenceCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}