using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.Many
{
    public class ManyAlgoResult : IAlgoResult
    {
        public ManyAlgoResult(IEnumerable<IAlgoResult> results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }

        public ManyAlgoResult(params IAlgoResult[] results)
        {
            Results = results;
        }

        public IEnumerable<IAlgoResult> Results { get; }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoResultExecutor<ManyAlgoResult>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}