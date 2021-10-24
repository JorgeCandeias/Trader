using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.ClearOpenOrders
{
    public class ClearOpenOrdersAlgoResult : IAlgoResult
    {
        public ClearOpenOrdersAlgoResult(Symbol symbol, OrderSide side)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Side = side;
        }

        public Symbol Symbol { get; }
        public OrderSide Side { get; }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoResultExecutor<ClearOpenOrdersAlgoResult>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}