using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.Many
{
    public class ManyCommand : IAlgoCommand
    {
        public ManyCommand(IEnumerable<IAlgoCommand> commands)
        {
            Commands = commands ?? throw new ArgumentNullException(nameof(commands));
        }

        public ManyCommand(params IAlgoCommand[] results)
        {
            Commands = results;
        }

        public IEnumerable<IAlgoCommand> Commands { get; }

        public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<ManyCommand>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}