using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;
using static System.String;

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
            if (!IsNullOrEmpty(context.Symbol.BaseAsset))
            {
                context.BaseAssetSavingsBalance = await _savings
                    .GetPositionOrZeroAsync(context.Symbol.BaseAsset, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!IsNullOrEmpty(context.Symbol.QuoteAsset))
            {
                context.QuoteAssetSavingsBalance = await _savings
                    .GetPositionOrZeroAsync(context.Symbol.QuoteAsset, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}