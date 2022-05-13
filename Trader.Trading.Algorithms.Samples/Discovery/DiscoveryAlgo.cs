using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Samples.Discovery;

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
        var exchangeSymbols = Context.Exchange.Symbols
            .Where(x => x.Status == SymbolStatus.Trading)
            .Where(x => x.IsSpotTradingAllowed)
            .ToDictionary(x => x.Name);

        var candidateSymbols = exchangeSymbols
            .Where(x => options.QuoteAssets.Contains(x.Value.QuoteAsset))
            .Where(x => !options.IgnoreSymbols.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        // get all usable savings assets
        var savings = (await _savings.GetProductsAsync(cancellationToken))
            .Select(x => x.Asset)
            .ToHashSet();

        // get all savings positions
        var savingsPositions = (await _savings.GetBalancesAsync(cancellationToken))
            .ToDictionary(x => x.Asset);

        // get all usable swap pools
        var pools = await _swaps.GetSwapPoolsAsync(cancellationToken);

        // identify used symbols
        var usedSymbols = exchangeSymbols
            .Where(x => _dependencies.Symbols.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        // identify unused symbols
        var unusedSymbols = candidateSymbols
            .Where(x => !_dependencies.Symbols.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        // identify used base assets
        var usedBaseAssets = exchangeSymbols
            .Select(x => x.Value.BaseAsset)
            .ToHashSet();

        LogIdentifiedUnusedSymbols(TypeName, unusedSymbols.Select(x => x.Key).OrderBy(x => x));

        // identify unused symbols with savings
        if (options.ReportSavings)
        {
            var unusedWithSavings = candidateSymbols
                .Where(x => !_dependencies.Symbols.Contains(x.Key))
                .Where(x => savings.Contains(x.Value.BaseAsset))
                .Select(x => x.Key)
                .OrderBy(x => x);

            LogIdentifiedUnusedSymbolsWithSavings(TypeName, unusedWithSavings);
        }

        // identify unused symbols with swap pools
        if (options.ReportSwapPools)
        {
            var unusedWithSwapPools = candidateSymbols
                .Where(x => !_dependencies.Symbols.Contains(x.Key))
                .Where(x => pools.Any(p => p.Assets.Contains(x.Value.QuoteAsset) && p.Assets.Contains(x.Value.BaseAsset)))
                .Select(x => x.Key)
                .OrderBy(x => x);

            LogIdentifiedUnusedSymbolsWithSwapPools(TypeName, unusedWithSwapPools);
        }

        // identify used symbols without savings
        if (options.ReportSavings)
        {
            var usedWithoutSavings = exchangeSymbols
                .Where(x => _dependencies.Symbols.Contains(x.Key))
                .Where(x => !options.IgnoreSymbols.Contains(x.Key))
                .Where(x => !savings.Contains(x.Value.BaseAsset))
                .Select(x => x.Key)
                .OrderBy(x => x);

            LogIdentifiedUsedSymbolsWithoutSavings(TypeName, usedWithoutSavings);
        }

        // identify used symbols without swap pools
        if (options.ReportSwapPools)
        {
            var usedWithoutSwapPools = exchangeSymbols
                .Where(x => _dependencies.Symbols.Contains(x.Key))
                .Where(x => !options.IgnoreSymbols.Contains(x.Key))
                .Where(x => !(pools.Any(p => p.Assets.Contains(x.Value.QuoteAsset) && p.Assets.Contains(x.Value.BaseAsset))))
                .Select(x => x.Key)
                .OrderBy(x => x);

            LogIdentifiedUsedSymbolsWithoutSwapPools(TypeName, usedWithoutSwapPools);
        }

        // identify savings positions with no active symbols
        if (options.ReportSavings)
        {
            var leftovers = savingsPositions
                .Keys
                .Where(x => !usedBaseAssets.Contains(x));

            LogIdentifiedSavingsPositionsWithoutActiveSymbols(TypeName, leftovers.OrderBy(x => x));
        }

        return Noop();
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{TypeName} identified unused symbols with savings: {Symbols}")]
    private partial void LogIdentifiedUnusedSymbolsWithSavings(string typeName, IEnumerable<string> symbols);

    [LoggerMessage(1, LogLevel.Information, "{TypeName} identified unused symbols with swap pools: {Symbols}")]
    private partial void LogIdentifiedUnusedSymbolsWithSwapPools(string typeName, IEnumerable<string> symbols);

    [LoggerMessage(2, LogLevel.Information, "{TypeName} identified used symbols without savings: {Symbols}")]
    private partial void LogIdentifiedUsedSymbolsWithoutSavings(string typeName, IEnumerable<string> symbols);

    [LoggerMessage(3, LogLevel.Information, "{TypeName} identified used symbols without swap pools: {Symbols}")]
    private partial void LogIdentifiedUsedSymbolsWithoutSwapPools(string typeName, IEnumerable<string> symbols);

    [LoggerMessage(4, LogLevel.Information, "{TypeName} identified used symbols without savings or swap pools: {Symbols}")]
    private partial void LogIdentifiedUsedSymbolsWithoutSavingsOrSwapPools(string typeName, IEnumerable<string> symbols);

    [LoggerMessage(5, LogLevel.Information, "{TypeName} identified unused symbols: {Symbols}")]
    private partial void LogIdentifiedUnusedSymbols(string typeName, IEnumerable<string> symbols);

    [LoggerMessage(6, LogLevel.Information, "{TypeName} identified savings positions without used symbols: {Assets}")]
    private partial void LogIdentifiedSavingsPositionsWithoutActiveSymbols(string typeName, IEnumerable<string> assets);

    #endregion Logging
}