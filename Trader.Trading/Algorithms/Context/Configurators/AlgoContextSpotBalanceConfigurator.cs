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
        foreach (var symbol in context.Symbols)
        {
            var spot = context.Data.GetOrAdd(symbol.Name).Spot;

            spot.BaseAsset = await _balances
                .GetBalanceOrZeroAsync(symbol.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            spot.QuoteAsset = await _balances
                .GetBalanceOrZeroAsync(symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}