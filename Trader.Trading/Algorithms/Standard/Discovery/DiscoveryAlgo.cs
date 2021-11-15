using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Discovery
{
    internal partial class DiscoveryAlgo : Algo
    {
        private readonly IOptionsMonitor<DiscoveryAlgoOptions> _monitor;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IAlgoDependencyResolver _dependencies;
        private readonly ISwapPoolProvider _swaps;

        public DiscoveryAlgo(IOptionsMonitor<DiscoveryAlgoOptions> monitor, ILogger<DiscoveryAlgo> logger, ITradingService trader, IAlgoDependencyResolver dependencies, ISwapPoolProvider swaps)
        {
            _monitor = monitor;
            _logger = logger;
            _trader = trader;
            _dependencies = dependencies;
            _swaps = swaps;
        }

        private static string TypeName { get; } = nameof(DiscoveryAlgo);

        protected override async ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            var options = _monitor.Get(Context.Name);

            // get the exchange info
            var symbols = Context.ExchangeInfo.Symbols
                .Where(x => x.Status == SymbolStatus.Trading)
                .Where(x => x.IsSpotTradingAllowed)
                .Where(x => options.QuoteAssets.Contains(x.QuoteAsset))
                .Where(x => !options.IgnoreSymbols.Contains(x.Name))
                .ToList();

            // get all usable savings assets
            var assets = (await _trader.GetSavingsProductsAsync(SavingsStatus.Subscribable, SavingsFeatured.All, cancellationToken).ConfigureAwait(false))
                .Where(x => x.CanPurchase)
                .Select(x => x.Asset)
                .Union(options.ForcedAssets)
                .ToHashSet();

            // get all usable swap pools
            var pools = await _swaps.GetSwapPoolsAsync(cancellationToken);

            // identify all symbols with savings
            var withSavings = symbols
                .Where(x => assets.Contains(x.QuoteAsset) && assets.Contains(x.BaseAsset))
                .Select(x => x.Name)
                .ToHashSet();

            // identify all symbols with swap pools
            var withSwapPools = symbols
                .Where(x => pools.Any(p => p.Assets.Contains(x.QuoteAsset) && p.Assets.Contains(x.BaseAsset)))
                .Select(x => x.Name)
                .ToHashSet();

            // get all symbols in use
            var used = _dependencies.AllSymbols;

            // identify unused symbols with savings
            var unusedWithSavings = withSavings
                .Except(used)
                .ToList();

            LogIdentifiedUnusedSymbolsWithSavings(TypeName, unusedWithSavings.Count, unusedWithSavings);

            // identify unused symbols with swap pools
            var unusedWithSwapPools = withSwapPools
                .Except(used)
                .ToList();

            LogIdentifiedUnusedSymbolsWithSwapPools(TypeName, unusedWithSwapPools.Count, unusedWithSwapPools);

            // identify used symbols without savings
            var usedWithoutSavings = used
                .Except(options.IgnoreSymbols)
                .Except(withSavings)
                .ToList();

            LogIdentifiedUsedSymbolsWithoutSavings(TypeName, usedWithoutSavings.Count, usedWithoutSavings);

            // identify used symbols without swap pools
            var usedWithoutSwapPools = used
                .Except(options.IgnoreSymbols)
                .Except(withSwapPools)
                .ToList();

            LogIdentifiedUsedSymbolsWithoutSwapPools(TypeName, usedWithoutSwapPools.Count, usedWithoutSwapPools);

            // identify used symbols without savings or swap pools
            var risky = used
                .Except(options.IgnoreSymbols)
                .Except(withSavings)
                .Except(withSwapPools)
                .ToList();

            LogIdentifiedUsedSymbolsWithoutSavingsOrSwapPools(TypeName, risky.Count, risky);

            return Noop();
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified {Count} unused symbols with savings: {Symbols}")]
        private partial void LogIdentifiedUnusedSymbolsWithSavings(string typeName, int count, IEnumerable<string> symbols);

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified {Count} unused symbols with swap pools: {Symbols}")]
        private partial void LogIdentifiedUnusedSymbolsWithSwapPools(string typeName, int count, IEnumerable<string> symbols);

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified {Count} used symbols without savings: {Symbols}")]
        private partial void LogIdentifiedUsedSymbolsWithoutSavings(string typeName, int count, IEnumerable<string> symbols);

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified {Count} used symbols without swap pools: {Symbols}")]
        private partial void LogIdentifiedUsedSymbolsWithoutSwapPools(string typeName, int count, IEnumerable<string> symbols);

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified {Count} used symbols without savings or swap pools: {Symbols}")]
        private partial void LogIdentifiedUsedSymbolsWithoutSavingsOrSwapPools(string typeName, int count, IEnumerable<string> symbols);

        #endregion Logging
    }
}