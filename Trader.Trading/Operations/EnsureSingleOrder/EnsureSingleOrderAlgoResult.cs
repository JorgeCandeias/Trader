using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.EnsureSingleOrder
{
    public class EnsureSingleOrderAlgoResult : IAlgoResult
    {
        public EnsureSingleOrderAlgoResult(Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Side = side;
            Type = type;
            TimeInForce = timeInForce;
            Quantity = quantity;
            Price = price;
            RedeemSavings = redeemSavings;
        }

        public Symbol Symbol { get; }
        public OrderSide Side { get; }
        public OrderType Type { get; }
        public TimeInForce TimeInForce { get; }
        public decimal Quantity { get; }
        public decimal Price { get; }
        public bool RedeemSavings { get; }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoResultExecutor<EnsureSingleOrderAlgoResult>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}