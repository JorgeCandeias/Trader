using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.CancelOrder
{
    public class CancelOrderCommand : IAlgoCommand
    {
        public CancelOrderCommand(Symbol symbol, long orderId)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            OrderId = orderId;
        }

        public Symbol Symbol { get; }
        public long OrderId { get; }

        public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<CancelOrderCommand>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}