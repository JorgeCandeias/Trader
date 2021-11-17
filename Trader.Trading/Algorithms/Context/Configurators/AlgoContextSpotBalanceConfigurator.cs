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
            if (!context.SpotBalances.TryGetValue(symbol.Key, out var balances))
            {
                context.SpotBalances[symbol.Key] = balances = new SymbolSpotBalances();
            }

            balances.Symbol = symbol.Value;

            balances.BaseAsset = await _balances
                .GetBalanceOrZeroAsync(symbol.Value.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            balances.QuoteAsset = await _balances
                .GetBalanceOrZeroAsync(symbol.Value.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}