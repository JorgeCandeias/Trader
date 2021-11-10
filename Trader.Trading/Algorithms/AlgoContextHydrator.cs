﻿using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoContextHydrator : IAlgoContextHydrator
    {
        private readonly IExchangeInfoProvider _exchange;
        private readonly IAutoPositionResolver _resolver;
        private readonly ITickerProvider _tickers;
        private readonly IBalanceProvider _balances;
        private readonly ISavingsProvider _savings;
        private readonly IOrderProvider _orders;
        private readonly ISwapPoolProvider _swaps;
        private readonly IEnumerable<IAlgoContextConfigurator<AlgoContext>> _configurators;

        public AlgoContextHydrator(IExchangeInfoProvider exchange, IAutoPositionResolver resolver, ITickerProvider tickers, IBalanceProvider balances, ISavingsProvider savings, IOrderProvider orders, ISwapPoolProvider swaps, IEnumerable<IAlgoContextConfigurator<AlgoContext>> configurators)
        {
            _exchange = exchange;
            _resolver = resolver;
            _tickers = tickers;
            _balances = balances;
            _savings = savings;
            _orders = orders;
            _swaps = swaps;
            _configurators = configurators;
        }

        public Task HydrateSymbolAsync(AlgoContext context, string name, string symbol, CancellationToken cancellationToken = default)
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

        public Task HydrateAllAsync(AlgoContext context, string name, string symbol, DateTime startTime, CancellationToken cancellationToken = default)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            return HydrateAllCoreAsync(context, name, symbol, cancellationToken);
        }

        private async Task HydrateSymbolCoreAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            context.Symbol = await _exchange
                .GetRequiredSymbolAsync(symbol, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HydrateAllCoreAsync(AlgoContext context, string name, string symbol, CancellationToken cancellationToken = default)
        {
            foreach (var configurator in _configurators)
            {
                await configurator
                    .ConfigureAsync(context, name, cancellationToken)
                    .ConfigureAwait(false);
            }

            var assetSavingsTask = _savings.GetPositionOrZeroAsync(context.Symbol.BaseAsset, cancellationToken);

            var quoteSavingsTask = _savings.GetPositionOrZeroAsync(context.Symbol.QuoteAsset, cancellationToken);

            var assetSwapPoolTask = _swaps.GetBalanceAsync(context.Symbol.BaseAsset, cancellationToken);

            var quoteSwapPoolTask = _swaps.GetBalanceAsync(context.Symbol.QuoteAsset, cancellationToken);

            var ordersTask = _orders.GetOrdersAsync(symbol, CancellationToken.None);

            // populate the asset savings balance
            context.AssetSavingsBalance = await assetSavingsTask.ConfigureAwait(false);

            // populate the quote savings balance
            context.QuoteSavingsBalance = await quoteSavingsTask.ConfigureAwait(false);

            // populate the asset swap pool balance
            context.AssetSwapPoolBalance = await assetSwapPoolTask.ConfigureAwait(false);

            // populate the quote swap pool balance
            context.QuoteSwapPoolBalance = await quoteSwapPoolTask.ConfigureAwait(false);

            // populate orders
            context.Orders = await ordersTask.ConfigureAwait(false);
        }
    }
}