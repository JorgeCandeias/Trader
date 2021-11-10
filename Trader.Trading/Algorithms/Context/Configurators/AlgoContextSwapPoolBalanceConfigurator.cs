using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextSwapPoolBalanceConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly ISwapPoolProvider _swaps;

        public AlgoContextSwapPoolBalanceConfigurator(ISwapPoolProvider swaps)
        {
            _swaps = swaps;
        }

        public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            context.AssetSwapPoolBalance = await _swaps
                .GetBalanceAsync(context.Symbol.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            context.QuoteSwapPoolBalance = await _swaps
                .GetBalanceAsync(context.Symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}