using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.TrackingBuy;

public class TrackingBuyCommand : IAlgoCommand
{
    public TrackingBuyCommand(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        PullbackRatio = pullbackRatio;
        TargetQuoteBalanceFractionPerBuy = targetQuoteBalanceFractionPerBuy;
        MaxNotional = maxNotional;
    }

    public Symbol Symbol { get; }
    public decimal PullbackRatio { get; }
    public decimal TargetQuoteBalanceFractionPerBuy { get; }
    public decimal? MaxNotional { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<TrackingBuyCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}