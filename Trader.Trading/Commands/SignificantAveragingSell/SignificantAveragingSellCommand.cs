using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.SignificantAveragingSell;

public class SignificantAveragingSellCommand : IAlgoCommand
{
    public SignificantAveragingSellCommand(Symbol symbol, decimal minimumProfitRate, bool redeemSavings, bool redeemSwapPool)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        MinimumProfitRate = minimumProfitRate;
        RedeemSavings = redeemSavings;
        RedeemSwapPool = redeemSwapPool;
    }

    public Symbol Symbol { get; }
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