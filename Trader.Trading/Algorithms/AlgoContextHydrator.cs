using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoContextHydrator : IAlgoContextHydrator
    {
        private readonly IExchangeInfoProvider _exchange;
        private readonly ISignificantOrderResolver _resolver;

        public AlgoContextHydrator(IExchangeInfoProvider exchange, ISignificantOrderResolver resolver)
        {
            _exchange = exchange;
            _resolver = resolver;
        }

        public Task HydrateAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            return HydrateCoreAsync(context, symbol, cancellationToken);
        }

        public Task HydrateAsync(AlgoContext context, string symbol, DateTime tick, CancellationToken cancellationToken = default)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            return HydrateCoreAsync(context, symbol, tick, cancellationToken);
        }

        private async Task<Symbol> HydrateCoreAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _exchange.GetRequiredSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);

            context.Symbol = result;

            return result;
        }

        private async Task HydrateCoreAsync(AlgoContext context, string symbol, DateTime tick, CancellationToken cancellationToken = default)
        {
            // populate new symbol information
            var symbolx = await HydrateCoreAsync(context, symbol, cancellationToken).ConfigureAwait(false);

            // populate significant assets
            context.Significant = await _resolver.ResolveAsync(symbolx, cancellationToken);
        }
    }
}