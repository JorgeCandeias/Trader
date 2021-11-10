using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextSpotBalanceConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly IBalanceProvider _balances;

        public AlgoContextSpotBalanceConfigurator(IBalanceProvider balances)
        {
            _balances = balances;
        }

        public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            context.AssetSpotBalance = await _balances
                .GetBalanceOrZeroAsync(context.Symbol.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            context.QuoteSpotBalance = await _balances
                .GetBalanceOrZeroAsync(context.Symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}