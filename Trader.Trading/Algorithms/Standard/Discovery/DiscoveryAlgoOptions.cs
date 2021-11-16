namespace Outcompute.Trader.Trading.Algorithms.Standard.Discovery;

public class DiscoveryAlgoOptions
{
    /// <summary>
    /// The assets used as quote for symbol discovery.
    /// </summary>
    public ISet<string> QuoteAssets { get; } = new HashSet<string>();

    /// <summary>
    /// Specific symbols to ignore.
    /// </summary>
    public ISet<string> IgnoreSymbols { get; } = new HashSet<string>();

    /// <summary>
    /// Whether to report discovery about savings assets.
    /// </summary>
    public bool ReportSavings { get; set; } = false;

    /// <summary>
    /// Whether to report discovery about swap pools.
    /// </summary>
    public bool ReportSwapPools { get; set; } = false;
}