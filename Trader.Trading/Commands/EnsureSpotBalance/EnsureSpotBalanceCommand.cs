using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.EnsureSpotBalance;

internal class EnsureSpotBalanceCommand : IAlgoCommand
{
    /// <summary>
    /// Creates a new command that will ensure the specified spot balance is available by redeeming enough assets from redemption sources.
    /// </summary>
    /// <param name="asset">The asset to evaluate.</param>
    /// <param name="value">The desired minimum spot balance for the asset.</param>
    /// <param name="redeemSavings">Whether to redeem savings.</param>
    /// <param name="redeemSwapPools">Whether to redeem from swap pools.</param>
    /// <param name="lockedAsFree">Whether to consider the locked amount as free amount. Use this if you are going to be replacing an existing order.</param>
    public EnsureSpotBalanceCommand(string asset, decimal value, bool redeemSavings = false, bool redeemSwapPools = false, bool lockedAsFree = false)
    {
        Guard.IsNotNull(asset, nameof(asset));
        Guard.IsGreaterThanOrEqualTo(value, 0, nameof(value));

        Asset = asset;
        Value = value;
        RedeemSavings = redeemSavings;
        RedeemSwapPools = redeemSwapPools;
        LockedAsFree = lockedAsFree;
    }

    public string Asset { get; }
    public decimal Value { get; }
    public bool RedeemSavings { get; }
    public bool RedeemSwapPools { get; }
    public bool LockedAsFree { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(context, nameof(context));

        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<EnsureSpotBalanceCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}