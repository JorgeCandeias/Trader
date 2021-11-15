using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.MarketBuy
{
    internal class MarketBuyCommand : IAlgoCommand
    {
        public MarketBuyCommand(Symbol symbol, decimal quantity, bool redeemSavings = false, bool redeemSwapPool = false)
        {
            Symbol = symbol;
            Quantity = quantity;
            RedeemSavings = redeemSavings;
            RedeemSwapPool = redeemSwapPool;
        }

        public Symbol Symbol { get; }
        public decimal Quantity { get; }
        public bool RedeemSavings { get; }
        public bool RedeemSwapPool { get; }

        public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<MarketBuyCommand>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}