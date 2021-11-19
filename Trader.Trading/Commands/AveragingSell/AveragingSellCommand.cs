using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.AveragingSell;

public class AveragingSellCommand : IAlgoCommand
{
    public AveragingSellCommand(Symbol symbol, decimal minSellRate, bool redeemSavings = false, bool redeemSwapPool = false)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        MinSellRate = minSellRate;
        RedeemSavings = redeemSavings;
        RedeemSwapPool = redeemSwapPool;
    }

    public Symbol Symbol { get; }
    public decimal MinSellRate { get; }
    public bool RedeemSavings { get; }
    public bool RedeemSwapPool { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<AveragingSellCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}