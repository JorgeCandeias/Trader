using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Samples.Portfolio;

public class PortfolioAlgoOptions
{
    /// <summary>
    /// Pad buy orders with this rate (before adjustments) to ensure that the resulting quantity is sellable.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal FeeRate { get; set; } = 0.001M;

    [Required]
    public bool UseSavings { get; set; }

    [Required]
    public bool UseSwapPools { get; set; }

    [Required]
    public PortfolioSellingAlgoOptions Selling { get; } = new();

    [Required]
    public PortfolioBuyingAlgoOptions Buying { get; } = new();

    [Required]
    public PortfolioRmiAlgoOptions Rmi { get; } = new();
}

/// <summary>
/// Options related to buying behaviour.
/// </summary>
public class PortfolioBuyingAlgoOptions
{
    /// <summary>
    /// Whether buying is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The fraction of the quote balance to use for each buy order.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal BalanceRate { get; set; } = 0.001M;

    /// <summary>
    /// The cooldown period between consecutive buys.
    /// </summary>
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Symbols which will never be bought.
    /// </summary>
    public ISet<string> ExcludeSymbols { get; } = new HashSet<string>();

    /// <inheritdoc cref="PortfolioOpeningOptions"/>
    public PortfolioOpeningOptions Opening { get; } = new();
}

/// <summary>
/// Options related to opening behaviour (first buy).
/// </summary>
public class PortfolioOpeningOptions
{
    /// <summary>
    /// Whether opening is disabled for specific symbols.
    /// If a symbol is excluded, no buys will be placed for it if it does not have open positions already.
    /// Enable to this to let the algo close all positions for tha symbol over time.
    /// </summary>
    public ISet<string> ExcludeSymbols { get; } = new HashSet<string>();
}

/// <summary>
/// Options related to selling behaviour.
/// </summary>
public class PortfolioSellingAlgoOptions
{
    /// <summary>
    /// Whether selling is enabled at all.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Regular stop loss to ensure profit when the symbol exhibits flash spikes.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal StopLossRate { get; set; } = 0.01M;

    /// <summary>
    /// The minimum profit rate for the assets elected for selling.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal MinProfitRate { get; set; } = 0.01M;

    /// <summary>
    /// A trailing stop loss will be placed at this level of profit to guarantee some return regardless of other factors.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal TakeProfitTriggerRate { get; set; } = 0.10M;

    /// <summary>
    /// Symbols which will never be sold.
    /// </summary>
    public ISet<string> ExcludeSymbols { get; } = new HashSet<string>();
}

/// <summary>
/// RMI options.
/// </summary>
public class PortfolioRmiAlgoOptions
{
    /// <summary>
    /// RMI threshold to cross upwards for buying.
    /// </summary>
    [Required, Range(0, 100)]
    public decimal Low { get; set; } = 30M;

    /// <summary>
    /// RMI threshold to cross downwards for selling.
    /// </summary>
    [Required, Range(0, 100)]
    public decimal High1 { get; set; } = 70M;

    /// <summary>
    /// RMI threshold to activate a stop loss sell at <see cref="High1"/>.
    /// </summary>
    [Required, Range(0, 100), GreaterThan(nameof(High1))]
    public decimal High2 { get; set; } = 80M;

    /// <summary>
    /// RMI threshold at which to activate an aggressive percent stop loss to guarantee profit from fast spikes.
    /// </summary>
    [Required, Range(0, 100)]
    public decimal High3 { get; set; } = 90M;

    /// <summary>
    /// Momentum periods for RMI calculation.
    /// </summary>
    [Required, Range(1, 100)]
    public int MomentumPeriods { get; set; } = 3;

    /// <summary>
    /// Running Moving Average periods for RMI calculation.
    /// </summary>
    [Required, Range(1, 100)]
    public int RmaPeriods { get; set; } = 14;
}