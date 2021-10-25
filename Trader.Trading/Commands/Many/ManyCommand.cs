using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.Many
{
    public class ManyCommand : IAlgoCommand
    {
        public ManyCommand(IEnumerable<IAlgoCommand> results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }

        public ManyCommand(params IAlgoCommand[] results)
        {
            Results = results;
        }

        public IEnumerable<IAlgoCommand> Results { get; }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<ManyCommand>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}