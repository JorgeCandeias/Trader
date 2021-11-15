using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Commands.SignificantAveragingSell
{
    public class SignificantAveragingSellCommand : IAlgoCommand
    {
        public SignificantAveragingSellCommand(Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, bool redeemSwapPool)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
            Orders = orders ?? throw new ArgumentNullException(nameof(orders));
            MinimumProfitRate = minimumProfitRate;
            RedeemSavings = redeemSavings;
            RedeemSwapPool = redeemSwapPool;

            foreach (var order in orders)
            {
                if (order.Side != OrderSide.Buy)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Parameter '{nameof(orders)}' must only contain orders with side '{OrderSide.Buy}'");
                }
                else if (order.ExecutedQuantity <= 0m)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Parameter '{nameof(orders)}' must only contain orders with executed quantity greater than zero'");
                }
            }
        }

        public Symbol Symbol { get; }
        public MiniTicker Ticker { get; }
        public IReadOnlyCollection<OrderQueryResult> Orders { get; }
        public decimal MinimumProfitRate { get; }
        public bool RedeemSavings { get; }
        public bool RedeemSwapPool { get; }

        public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<SignificantAveragingSellCommand>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}