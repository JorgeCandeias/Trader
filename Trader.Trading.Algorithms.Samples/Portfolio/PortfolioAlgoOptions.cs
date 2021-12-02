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
    public PortfolioAlgoOptionsRecovery Recovery { get; } = new();

    [Required]
    public PortfolioAlgoOptionsSellOff SellOff { get; } = new();

    [Required]
    public PortfolioAlgoOptionsTopUpBuy TopUpBuy { get; } = new();

    [Required]
    public PortfolioAlgoOptionsEntryBuy EntryBuy { get; } = new();
}

/// <summary>
/// Options related to entry buying behaviour.
/// </summary>
public class PortfolioAlgoOptionsEntryBuy
{
    /// <summary>
    /// Whether entry buying is enabled.
    /// </summary>
    [Required]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The fraction of the quote balance to use for each buy order.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal BalanceRate { get; set; } = 0.001M;

    /// <inheritdoc cref="PortfolioAlgoOptionsEntryBuyRsi"/>
    [Required]
    public PortfolioAlgoOptionsEntryBuyRsi Rsi { get; } = new();
}

/// <summary>
/// RSI options related to entry buying behaviour.
/// </summary>
public class PortfolioAlgoOptionsEntryBuyRsi
{
    /// <summary>
    /// Periods for RSI calculation.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int Periods { get; set; } = 6;

    /// <summary>
    /// RSI threshold under which to perfrom entry buys.
    /// </summary>
    [Required, Range(0, 100)]
    public decimal Oversold { get; set; } = 20M;
}

/// <summary>
/// Options related to top up buying behaviour.
/// </summary>
public class PortfolioAlgoOptionsTopUpBuy
{
    /// <summary>
    /// Whether topping up is enabled.
    /// </summary>
    [Required]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The fraction of the quote balance to use for each buy order.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal BalanceRate { get; set; } = 0.001M;

    /// <summary>
    /// The minimum increase rate from the last lot position for a top up buy to execute.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal RaiseRate { get; set; } = 0.01M;

    /// <inheritdoc cref="PortfolioAlgoOptionsTopUpBuyRsi"/>
    [Required]
    public PortfolioAlgoOptionsTopUpBuyRsi Rsi { get; } = new();

    /// <inheritdoc cref="PortfolioAlgoOptionsTopUpBuySma"/>
    [Required]
    public PortfolioAlgoOptionsTopUpBuySma Sma { get; } = new();
}

/// <summary>
/// RSI options related to top up buying behaviour.
/// </summary>
public class PortfolioAlgoOptionsTopUpBuyRsi
{
    /// <summary>
    /// Whether the safety RSI check is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Periods for RSI calculation.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int Periods { get; set; } = 6;

    /// <summary>
    /// RSI threshold above which never to perform top up buys.
    /// </summary>
    [Required, Range(0, 100)]
    public decimal Overbought { get; set; } = 70M;
}

/// <summary>
/// SMA options related to top up buying behaviour.
/// </summary>
public class PortfolioAlgoOptionsTopUpBuySma
{
    /// <summary>
    /// Whether the safety SMA check is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The periods for SMA calculation.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int Periods { get; set; } = 7;
}

/// <summary>
/// Options related to sell off behaviour.
/// </summary>
public class PortfolioAlgoOptionsSellOff
{
    /// <summary>
    /// Whether sell off at high profit logic is enabled.
    /// </summary>
    [Required]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The minimum relative value (pv / cost) at which to sell off an asset as compared to its average cost.
    /// </summary>
    [Required, Range(1, double.MaxValue)]
    public decimal TriggerRate { get; set; } = 2M;

    /// <summary>
    /// The distance to the target sell off price at which point to prepare the limit order.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal OrderPriceRange { get; set; } = 0.01M;

    /// <summary>
    /// Symbols which will never be sold off.
    /// </summary>
    public ISet<string> ExcludeSymbols { get; } = new HashSet<string>();

    /// <inheritdoc cref="PortfolioAlgoOptionsSellOffRsi"/>
    public PortfolioAlgoOptionsSellOffRsi Rsi { get; } = new();
}

/// <summary>
/// RSI options related to sell off behaviour.
/// </summary>
public class PortfolioAlgoOptionsSellOffRsi
{
    /// <summary>
    /// The periods used for sell off RSI calculation.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int Periods { get; set; } = 12;

    /// <summary>
    /// The RSI threshold above which to attempt a sell.
    /// </summary>
    [Required, Range(0, 100)]
    public decimal Overbought { get; set; } = 80M;
}

/// <summary>
/// Options related to recovery behaviour.
/// </summary>
public class PortfolioAlgoOptionsRecovery
{
    /// <summary>
    /// Whether recovery logic is enabled.
    /// </summary>
    [Required]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Drop rate from the last buy for recovery to activate.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal DropRate { get; set; } = 0.10M;

    /// <summary>
    /// The fraction of the quote balance to use for each buy order.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal BalanceRate { get; set; } = 0.001M;

    /// <summary>
    /// The distance to the target recovery buy price at which point to prepare the limit order.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal BuyOrderPriceRange { get; set; } = 0.01M;

    /// <summary>
    /// The cooldown between sequential recovery buys.
    /// </summary>
    [Required, Range(typeof(TimeSpan), "0.00:00:00.000", "360.00:00:00.000")]
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromDays(1);

    /// <inheritdoc cref="PortfolioAlgoOptionsRecoveryRsi"/>
    [Required]
    public PortfolioAlgoOptionsRecoveryRsi Rsi { get; } = new();

    /// <summary>
    /// Symbols which will never be recovered.
    /// </summary>
    public ISet<string> ExcludeSymbols { get; } = new HashSet<string>();
}

/// <summary>
/// RSI options related to recovery behaviour.
/// </summary>
public class PortfolioAlgoOptionsRecoveryRsi
{
    /// <summary>
    /// RSI threshold under which to perfrom recovery buys.
    /// </summary>
    [Required, Range(0, 100)]
    public decimal Buy { get; set; } = 10M;

    /// <summary>
    /// RSI threshold above which to perfrom recovery sells.
    /// </summary>
    [Required, Range(0, 100)]
    public decimal Sell { get; set; } = 70M;

    /// <summary>
    /// Periods for RSI calculation.
    /// </summary>
    [Required, Range(0, 100)]
    public int Periods { get; set; } = 6;
}