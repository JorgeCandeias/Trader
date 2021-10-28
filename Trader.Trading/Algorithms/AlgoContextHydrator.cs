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
        private readonly ITickerProvider _tickers;
        private readonly IBalanceProvider _balances;

        public AlgoContextHydrator(IExchangeInfoProvider exchange, ISignificantOrderResolver resolver, ITickerProvider tickers, IBalanceProvider balances)
        {
            _exchange = exchange;
            _resolver = resolver;
            _tickers = tickers;
            _balances = balances;
        }

        public Task HydrateSymbolAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            return HydrateSymbolCoreAsync(context, symbol, cancellationToken);
        }

        public Task HydrateAllAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            return HydrateAllCoreAsync(context, symbol, cancellationToken);
        }

        private async Task HydrateSymbolCoreAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            context.Symbol = await _exchange
                .GetRequiredSymbolAsync(symbol, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HydrateAllCoreAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            // fetch all required information in parallel as much as possible
            var symbolTask = _exchange
                .GetRequiredSymbolAsync(symbol, cancellationToken);

            var significantTask = symbolTask
                .ContinueWith(symbolx => _resolver.ResolveAsync(symbolx.Result, cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                .Unwrap();

            var tickerTask = _tickers
                .GetRequiredTickerAsync(symbol, cancellationToken);

            var assetBalanceTask = symbolTask
                .ContinueWith(symbolx => _balances.GetBalanceOrZeroAsync(symbolx.Result.BaseAsset, cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                .Unwrap();

            var quoteBalanceTask = symbolTask
                .ContinueWith(symbolx => _balances.GetBalanceOrZeroAsync(symbolx.Result.QuoteAsset, cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                .Unwrap();

            // populate the symbol
            context.Symbol = await symbolTask.ConfigureAwait(false);

            // populate significant assets
            context.Significant = await significantTask.ConfigureAwait(false);

            // populate the current ticker
            context.Ticker = await tickerTask.ConfigureAwait(false);

            // populate the asset spot balance
            context.AssetBalance = await assetBalanceTask.ConfigureAwait(false);

            // populate the quote spot balance
            context.QuoteBalance = await quoteBalanceTask.ConfigureAwait(false);
        }
    }
}