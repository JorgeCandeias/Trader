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

        public GridAlgo(ILogger<GridAlgo> logger, IOptions<GridAlgoOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        private static string TypeName => nameof(GridAlgo);

        /// <summary>
        /// Keeps track of the bands managed by the algorithm.
        /// </summary>
        private readonly SortedSet<Band> _bands = new(BandComparer.Default);

        public override async Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            var transientSellOrders = Context.Orders
                .Where(x => x.Side == OrderSide.Sell && x.Status.IsTransientStatus());

            // start fresh for this tick - later on we can optimize with diffs
            _bands.Clear();

            return
                TryApplySignificantBuyOrders() ??
                TryApplyNonSignificantOpenBuyOrders() ??
                TryMergeLeftoverBands() ??
                TryAdjustBandClosePrices() ??
                TryApplyOpenSellOrders() ??
                await TrySetStartingTradeAsync(cancellationToken) ??
                TryCancelRogueSellOrders() ??
                TryCancelExcessSellOrders(transientSellOrders) ??
                await TrySetBandSellOrdersAsync(transientSellOrders, cancellationToken) ??
                await TryCreateLowerBandOrderAsync(cancellationToken) ??
                TryCloseOutOfRangeBands() ??
                Noop();
        }

        private decimal GetFreeBalance()
        {
            return Context.QuoteSpotBalance.Free + (_options.UseQuoteSavings ? Context.QuoteSavingsBalance.FreeAmount : 0m);
        }

        private static string CreateTag(string symbol, decimal price)
        {
            return $"{symbol}{price:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);
        }
    }
}