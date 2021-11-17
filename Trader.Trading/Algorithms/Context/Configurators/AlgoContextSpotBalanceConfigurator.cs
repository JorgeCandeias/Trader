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
            var baseAsset = await _balances
                .GetBalanceOrZeroAsync(symbol.Value.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            var quoteAsset = await _balances
                .GetBalanceOrZeroAsync(symbol.Value.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);

            context.SpotBalancesLookup[symbol.Key] = new SymbolSpotBalances(symbol.Key, baseAsset, quoteAsset);
        }
    }
}