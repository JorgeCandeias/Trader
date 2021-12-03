using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Commands.EnsureSpotBalance;

internal partial class EnsureSpotBalanceExecutor : IAlgoCommandExecutor<EnsureSpotBalanceCommand>
{
    private readonly ILogger _logger;
    private readonly IBalanceProvider _balances;
    private readonly ISavingsProvider _savings;

    public EnsureSpotBalanceExecutor(ILogger<EnsureSpotBalanceExecutor> logger, IBalanceProvider balances, ISavingsProvider savings)
    {
        _logger = logger;
        _balances = balances;
        _savings = savings;
    }

    private const string TypeName = nameof(EnsureSpotBalanceCommand);

    public async ValueTask<EnsureSpotBalanceEvent> ExecuteAsync(IAlgoContext context, EnsureSpotBalanceCommand command, CancellationToken cancellationToken = default)
    {
        // get the spot balance for the specified asset
        var balance = await _balances.TryGetBalanceAsync(command.Asset, cancellationToken);
        var spot = balance?.Free ?? 0M;

        // if the free balance already covers the target amount then we can stop here
        if (spot >= command.Value)
        {
            return new EnsureSpotBalanceEvent(true, 0);
        }

        // if not then calculate the required amount to redeem
        var required = command.Value - spot;
        LogWillRedeem(TypeName, context.Name, spot, command.Asset, required, command.Value);

        // attempt to redeem from savings first
        var redeemed = 0M;
        if (command.RedeemSavings)
        {
            var result = await _savings.RedeemAsync(command.Asset, required, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                required -= Math.Min(result.Redeemed, required);
                redeemed += result.Redeemed;
                LogRedeemedFromSavings(TypeName, context.Name, result.Redeemed, command.Asset, command.Value, required);
            }
            else
            {
                LogCouldNotRedeemFromSavings(TypeName, context.Name, required, command.Asset);
            }
        }

        // if we redeemed enough then we can stop here
        if (required <= 0)
        {
            return new EnsureSpotBalanceEvent(true, redeemed);
        }

        // attempt to redeem from a swap pool
        if (command.RedeemSwapPools)
        {
            var result = await new RedeemSwapPoolCommand(command.Asset, required)
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);

            if (result.Success)
            {
                required -= Math.Min(result.QuoteAmount, required);
                redeemed += result.QuoteAmount;
                LogRedeemedFromSwapPool(TypeName, context.Name, result.QuoteAmount, result.QuoteAsset, command.Value, required);
            }
            else
            {
                LogCouldNotRedeemFromSwapPool(TypeName, context.Name, required, command.Asset);
            }
        }

        return new EnsureSpotBalanceEvent(required <= 0, redeemed);
    }

    async ValueTask IAlgoCommandExecutor<EnsureSpotBalanceCommand>.ExecuteAsync(IAlgoContext context, EnsureSpotBalanceCommand command, CancellationToken cancellationToken)
    {
        await ExecuteAsync(context, command, cancellationToken).ConfigureAwait(false);
    }

    #region Logging

    [LoggerMessage(1, LogLevel.Information, "{Type} {Name} reports free spot balance is only {Free:F8} {Asset} and will attempt to redeem {Required:F8} {Asset} to reach the total of {Total:F8} {Asset}")]
    private partial void LogWillRedeem(string type, string name, decimal free, string asset, decimal required, decimal total);

    [LoggerMessage(2, LogLevel.Information, "{Type} {Name} redeemed {Redeemed:F8} {Asset} from savings to reach total of {Total:F8} {Asset} with {Required:F8} {Asset} to go")]
    private partial void LogRedeemedFromSavings(string type, string name, decimal redeemed, string asset, decimal total, decimal required);

    [LoggerMessage(3, LogLevel.Warning, "{Type} {Name} could not redeem {Required:F8} {Asset} from savings")]
    private partial void LogCouldNotRedeemFromSavings(string type, string name, decimal required, string asset);

    [LoggerMessage(4, LogLevel.Information, "{Type} {Name} redeemed {Redeemed:F8} {Asset} from a swap pool to reach total of {Total:F8} {Asset} with {Required:F8} {Asset} to go")]
    private partial void LogRedeemedFromSwapPool(string type, string name, decimal redeemed, string asset, decimal total, decimal required);

    [LoggerMessage(5, LogLevel.Warning, "{Type} {Name} could not redeem {Required:F8} {Asset} from a swap pool")]
    private partial void LogCouldNotRedeemFromSwapPool(string type, string name, decimal required, string asset);

    #endregion Logging
}