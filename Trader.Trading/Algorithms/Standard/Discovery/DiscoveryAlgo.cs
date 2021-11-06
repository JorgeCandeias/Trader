using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Discovery
{
    internal class DiscoveryAlgo : Algo
    {
        private readonly IOptionsMonitor<DiscoveryAlgoOptions> _monitor;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IOptionsMonitor<AlgoDependencyOptions> _dependencies;
        private readonly IExchangeInfoProvider _info;

        public DiscoveryAlgo(IOptionsMonitor<DiscoveryAlgoOptions> monitor, ILogger<DiscoveryAlgo> logger, ITradingService trader, IOptionsMonitor<AlgoDependencyOptions> dependencies, IExchangeInfoProvider info)
        {
            _monitor = monitor;
            _logger = logger;
            _trader = trader;
            _dependencies = dependencies;
            _info = info;
        }

        public override async Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            var options = _monitor.Get(Context.Name);

            // get the exchange info
            var symbols = (await _info.GetExchangeInfoAsync(cancellationToken).ConfigureAwait(false)).Symbols;

            // get all usable savings assets
            var assets = (await _trader.GetSubscribableSavingsProductsAsync(cancellationToken).ConfigureAwait(false))
                .Where(x => x.CanPurchase)
                .Select(x => x.Asset)
                .ToHashSet();

            // get all symbols in use
            var used = _dependencies.CurrentValue.Symbols;

            // identify unused symbols with savings
            var candidates = symbols
                .Where(x => options.QuoteAssets.Contains(x.QuoteAsset))
                .Where(x => !options.QuoteAssets.Contains(x.BaseAsset))
                .Where(x => assets.Contains(x.BaseAsset) && assets.Contains(x.QuoteAsset))
                .Select(x => x.Name)
                .Except(used)
                .ToList();

            _logger.LogInformation(
                "{Type} identified {Count} unused symbols with savings: {Symbols}",
                nameof(DiscoveryAlgo), candidates.Count, candidates);

            return Noop();
        }
    }
}