using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.EnsureSpotBalance;

internal class EnsureSpotBalanceCommand : IAlgoCommand
{
    public EnsureSpotBalanceCommand(string asset, decimal value, bool redeemSavings, bool redeemSwapPools)
    {
        Guard.IsNotNull(asset, nameof(asset));
        Guard.IsGreaterThanOrEqualTo(value, 0, nameof(value));

        Asset = asset;
        Value = value;
        RedeemSavings = redeemSavings;
        RedeemSwapPools = redeemSwapPools;
    }

    public string Asset { get; }
    public decimal Value { get; }
    public bool RedeemSavings { get; }
    public bool RedeemSwapPools { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(context, nameof(context));

        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<EnsureSpotBalanceCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}