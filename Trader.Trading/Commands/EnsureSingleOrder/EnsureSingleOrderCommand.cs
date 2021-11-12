using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.EnsureSingleOrder
{
    public class EnsureSingleOrderCommand : IAlgoCommand
    {
        public EnsureSingleOrderCommand(Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, bool redeemSwapPool)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Side = side;
            Type = type;
            TimeInForce = timeInForce;
            Quantity = quantity;
            Price = price;
            RedeemSavings = redeemSavings;
            RedeemSwapPool = redeemSwapPool;

            if (side != OrderSide.Buy && side != OrderSide.Sell) throw new ArgumentOutOfRangeException(nameof(side));
        }

        public Symbol Symbol { get; }
        public OrderSide Side { get; }
        public OrderType Type { get; }
        public TimeInForce TimeInForce { get; }
        public decimal Quantity { get; }
        public decimal Price { get; }
        public bool RedeemSavings { get; }
        public bool RedeemSwapPool { get; }

        public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<EnsureSingleOrderCommand>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}