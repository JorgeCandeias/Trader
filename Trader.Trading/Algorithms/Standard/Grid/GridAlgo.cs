using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo : SymbolAlgo
    {
        private readonly ILogger _logger;
        private readonly GridAlgoOptions _options;

        public GridAlgo(ILogger<GridAlgo> logger, IOptionsSnapshot<GridAlgoOptions> options)
        {
            _logger = logger;
            _options = options.Get(Context.Name);
        }

        private static string TypeName => nameof(GridAlgo);

        /// <summary>
        /// Keeps track of the bands managed by the algorithm.
        /// </summary>
        private readonly SortedSet<Band> _bands = new(BandComparer.Default);

        /// <summary>
        /// Caches the very few transient orders to optimize loops in all steps.
        /// </summary>
        private readonly SortedSet<OrderQueryResult> _transient = new(OrderQueryResult.KeyComparer);

        public override async Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            // start fresh for this tick - later on we can optimize with diffs
            _bands.Clear();

            // cache the few transient orders so we dont have to loop through thousands of orders multiple times just to use a couple
            _transient.Clear();
            _transient.UnionWith(Context.Orders.Where(x => x.Status.IsTransientStatus()));

            // evaluate rules one-by-one and let the first one win
            return
                TryApplySignificantBuyOrders() ??
                TryApplyNonSignificantOpenBuyOrders() ??
                TryMergeLeftoverBands() ??
                TryAdjustBandClosePrices() ??
                TryApplyOpenSellOrders() ??
                await TrySetStartingTradeAsync(cancellationToken) ??
                TryCancelRogueSellOrders() ??
                TryCancelExcessSellOrders() ??
                await TrySetBandSellOrdersAsync(cancellationToken) ??
                await TryCreateLowerBandOrderAsync(cancellationToken) ??
                TryCloseOutOfRangeBands() ??
                Noop();
        }

        private decimal GetFreeBalance()
        {
            return Context.QuoteSpotBalance.Free
                + (_options.UseQuoteSavings ? Context.QuoteSavingsBalance.FreeAmount : 0m)
                + (_options.UseQuoteSwapPool ? Context.QuoteSwapPoolBalance.Total : 0m);
        }

        private static string CreateTag(string symbol, decimal price)
        {
            return $"{symbol}{price:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);
        }
    }
}