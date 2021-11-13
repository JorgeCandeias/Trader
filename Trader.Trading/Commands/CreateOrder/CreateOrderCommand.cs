using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Commands.CreateOrder
{
    public class CreateOrderCommand : IAlgoCommand
    {
        public CreateOrderCommand(Symbol symbol, OrderType type, OrderSide side, TimeInForce? timeInForce, decimal quantity, decimal? price, string? tag)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Type = type;
            Side = side;
            TimeInForce = timeInForce;
            Quantity = quantity;
            Price = price;
            Tag = tag;
        }

        public Symbol Symbol { get; }
        public OrderType Type { get; }
        public OrderSide Side { get; }
        public TimeInForce? TimeInForce { get; }
        public decimal Quantity { get; }
        public decimal? Price { get; }
        public string? Tag { get; }

        public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<CreateOrderCommand>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}