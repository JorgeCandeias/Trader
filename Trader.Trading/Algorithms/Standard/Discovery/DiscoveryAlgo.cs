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
        private readonly IAlgoDependencyResolver _dependencies;
        private readonly ISavingsProvider _savings;
        private readonly ISwapPoolProvider _swaps;

        public DiscoveryAlgo(IOptionsMonitor<DiscoveryAlgoOptions> monitor, ILogger<DiscoveryAlgo> logger, IAlgoDependencyResolver dependencies, ISavingsProvider savings, ISwapPoolProvider swaps)
        {
            _monitor = monitor;
            _logger = logger;
            _dependencies = dependencies;
            _savings = savings;
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
                .ToHashSet();

            // get all usable savings assets
            var savings = (await _savings.GetProductsAsync(cancellationToken))
                .Select(x => x.Asset)
                .ToHashSet();

            // get all usable swap pools
            var pools = await _swaps.GetSwapPoolsAsync(cancellationToken);

            // identify all symbols with savings
            var withSavings = symbols
                .Where(x => savings.Contains(x.QuoteAsset) && savings.Contains(x.BaseAsset))
                .Select(x => x.Name)
                .ToHashSet();

            // identify all symbols with swap pools
            var withSwapPools = symbols
                .Where(x => pools.Any(p => p.Assets.Contains(x.QuoteAsset) && p.Assets.Contains(x.BaseAsset)))
                .Select(x => x.Name)
                .ToHashSet();

            // get all symbols in use
            var used = _dependencies.AllSymbols
                .Intersect(symbols.Select(x => x.Name))
                .ToHashSet();

            // identify unused symbols with savings
            var unusedWithSavings = withSavings
                .Except(used);

            LogIdentifiedUnusedSymbolsWithSavings(TypeName, unusedWithSavings);

            // identify unused symbols with swap pools
            var unusedWithSwapPools = withSwapPools
                .Except(used);

            LogIdentifiedUnusedSymbolsWithSwapPools(TypeName, unusedWithSwapPools);

            // identify used symbols without savings
            var usedWithoutSavings = used
                .Except(options.IgnoreSymbols)
                .Except(withSavings);

            LogIdentifiedUsedSymbolsWithoutSavings(TypeName, usedWithoutSavings);

            // identify used symbols without swap pools
            var usedWithoutSwapPools = used
                .Except(options.IgnoreSymbols)
                .Except(withSwapPools);

            LogIdentifiedUsedSymbolsWithoutSwapPools(TypeName, usedWithoutSwapPools);

            // identify used symbols without savings or swap pools
            var risky = used
                .Except(options.IgnoreSymbols)
                .Except(withSavings)
                .Except(withSwapPools);

            LogIdentifiedUsedSymbolsWithoutSavingsOrSwapPools(TypeName, risky);

            return Noop();
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified unused symbols with savings: {Symbols}")]
        private partial void LogIdentifiedUnusedSymbolsWithSavings(string typeName, IEnumerable<string> symbols);

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified unused symbols with swap pools: {Symbols}")]
        private partial void LogIdentifiedUnusedSymbolsWithSwapPools(string typeName, IEnumerable<string> symbols);

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified used symbols without savings: {Symbols}")]
        private partial void LogIdentifiedUsedSymbolsWithoutSavings(string typeName, IEnumerable<string> symbols);

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified used symbols without swap pools: {Symbols}")]
        private partial void LogIdentifiedUsedSymbolsWithoutSwapPools(string typeName, IEnumerable<string> symbols);

        [LoggerMessage(0, LogLevel.Information, "{TypeName} identified used symbols without savings or swap pools: {Symbols}")]
        private partial void LogIdentifiedUsedSymbolsWithoutSavingsOrSwapPools(string typeName, IEnumerable<string> symbols);

        #endregion Logging
    }
}