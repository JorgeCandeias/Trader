using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextSpotBalanceConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly IBalanceProvider _balances;

    public AlgoContextSpotBalanceConfigurator(IBalanceProvider balances)
    {
        _balances = balances;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        if (!IsNullOrEmpty(context.Symbol.Name))
        {
            context.BaseAssetSpotBalance = await _balances
                .GetBalanceOrZeroAsync(context.Symbol.BaseAsset, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!IsNullOrEmpty(context.Symbol.Name))
        {
            context.QuoteAssetSpotBalance = await _balances
                .GetBalanceOrZeroAsync(context.Symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}