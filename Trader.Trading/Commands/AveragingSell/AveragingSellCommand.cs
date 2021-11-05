using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.AveragingSell
{
    public class AveragingSellCommand : IAlgoCommand
    {
        public AveragingSellCommand(Symbol symbol, IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings, bool redeemSwapPool)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Orders = orders ?? throw new ArgumentNullException(nameof(orders));
            ProfitMultiplier = profitMultiplier;
            RedeemSavings = redeemSavings;
            RedeemSwapPool = redeemSwapPool;

            if (orders.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(orders), $"Orders parameter must not be empty");
            }

            var quantity = 0m;
            foreach (var order in orders)
            {
                if (order.Side != OrderSide.Buy)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Order {order.OrderId} is not a buy order");
                }
                else if (order.ExecutedQuantity <= 0m)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Order {order.OrderId} has non-significant executed quantity");
                }
                quantity += order.ExecutedQuantity;
            }

            if (quantity == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(orders), $"Sum of all {nameof(OrderQueryResult.ExecutedQuantity)} in orders must be greater than zero");
            }
        }

        public Symbol Symbol { get; }
        public IReadOnlyCollection<OrderQueryResult> Orders { get; }
        public decimal ProfitMultiplier { get; }
        public bool RedeemSavings { get; }
        public bool RedeemSwapPool { get; }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<AveragingSellCommand>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}