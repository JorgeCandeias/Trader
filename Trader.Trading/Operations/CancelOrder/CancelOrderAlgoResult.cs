using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.CancelOrder
{
    public class CancelOrderAlgoResult : IAlgoResult
    {
        public CancelOrderAlgoResult(Symbol symbol, long orderId)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            OrderId = orderId;
        }

        public Symbol Symbol { get; }
        public long OrderId { get; }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoResultExecutor<CancelOrderAlgoResult>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}