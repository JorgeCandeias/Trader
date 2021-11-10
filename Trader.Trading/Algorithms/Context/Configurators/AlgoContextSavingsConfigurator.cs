using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextSavingsConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly ISavingsProvider _savings;

        public AlgoContextSavingsConfigurator(ISavingsProvider savings)
        {
            _savings = savings;
        }

        public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            context.AssetSavingsBalance = await _savings
                .GetPositionOrZeroAsync(context.Symbol.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            context.QuoteSavingsBalance = await _savings
                .GetPositionOrZeroAsync(context.Symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}