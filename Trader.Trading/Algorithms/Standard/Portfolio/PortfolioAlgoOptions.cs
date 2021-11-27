using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Portfolio;

public class PortfolioAlgoOptions
{
    [Required, Range(0, 1)]
    public decimal BalanceFractionPerBuy { get; set; } = 0.001M;

    [Required, Range(0, 1)]
    public decimal BuyQuoteBalanceFraction { get; set; } = 0.001M;

    /// <summary>
    /// Pad buy orders with this rate (before adjustments) to ensure that the resulting quantity is sellable.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal FeeRate { get; set; } = 0.001M;

    /// <summary>
    /// Whether to enable stop loss logic.
    /// </summary>
    [Required]
    public bool StopLossEnabled { get; set; } = false;

    /// <summary>
    /// When the current price falls from the last position price by this rate, the stop loss will signal.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal StopLossRateFromLastPosition { get; set; } = 0.01M;

    /// <summary>
    /// When the average value of all sellable assets vs their average price falls, the stop loss logic will activate.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal StopLossRate { get; set; } = 0.10M;

    /// <summary>
    /// When stop loss is triggered, the sell order will only be placed if all assets can be sold at least at the this markup from their average cost.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal MinStopLossProfitRate { get; set; } = 0.01M;

    [Range(0, int.MaxValue)]
    public decimal? MaxNotional { get; set; }

    [Required, Range(typeof(TimeSpan), "0.00:00:01.000", "365.00:00:00.000")]
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromDays(1);

    [Required]
    public bool UseSavings { get; set; }

    [Required]
    public bool UseSwapPools { get; set; }

    [Required]
    public PortfolioAlgoOptionsRsis Rsi { get; } = new();

    [Required]
    public PortfolioAlgoOptionsRecovery Recovery { get; } = new();

    [Required]
    public PortfolioAlgoOptionsSellOff SellOff { get; } = new();

    [Required]
    public PortfolioAlgoOptionsTopUpBuy TopUpBuy { get; } = new();

    /// <summary>
    /// Set of symbols to never issue sells against, even for stop loss.
    /// </summary>
    public ISet<string> NeverSellSymbols { get; } = new HashSet<string>();
}

/// <summary>
/// Options related to top up buying behaviour.
/// </summary>
public class PortfolioAlgoOptionsTopUpBuy
{
    /// <summary>
    /// The minimum increase rate from the last lot position for a top up buy to execute.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal RaiseRate { get; set; } = 0.01M;

    [Required]
    public PortfolioAlgoOptionsTopBuyRsi Rsi { get; } = new();
}

/// <summary>
/// Rsi options related to top up buying behaviour.
/// </summary>
public class PortfolioAlgoOptionsTopBuyRsi
{
    /// <summary>
    /// Periods for RSI calculation.
    /// </summary>
    [Required]
    public int Periods { get; set; } = 6;

    /// <summary>
    /// RSI threshold above which never to perform top up buys.
    /// </summary>
    [Required]
    public decimal Overbought { get; set; } = 70M;

    /// <summary>
    /// Precision for RSI price prediction logic.
    /// </summary>
    [Required]
    public decimal Precision { get; set; } = 0.01M;
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
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Drop rate from the last buy for recovery to activate.
    /// </summary>
    [Required]
    public decimal DropRate { get; set; } = 0.10M;

    [Required]
    public PortfolioAlgoOptionsRecoveryRsi Rsi { get; } = new();
}

public class PortfolioAlgoOptionsRecoveryRsi
{
    /// <summary>
    /// RSI threshold under which to perfrom recovery buys.
    /// </summary>
    [Required]
    public decimal Buy { get; set; } = 20M;

    /// <summary>
    /// RSI threshold above which to perfrom recovery sells.
    /// </summary>
    [Required]
    public decimal Sell { get; set; } = 80M;

    /// <summary>
    /// Periods for RSI calculation.
    /// </summary>
    [Required]
    public int Periods { get; set; } = 6;

    /// <summary>
    /// Precision for RSI price prediction logic.
    /// </summary>
    [Required]
    public decimal Precision { get; set; } = 0.01M;
}

public class PortfolioAlgoOptionsRsis
{
    [Required]
    public PortfolioAlgoOptionsRsiBuy Buy { get; } = new PortfolioAlgoOptionsRsiBuy();

    [Required]
    public PortfolioAlgoOptionsRsiSell Sell { get; } = new PortfolioAlgoOptionsRsiSell();
}

public class PortfolioAlgoOptionsRsiBuy
{
    /// <summary>
    /// Periods for RSI calculation.
    /// </summary>
    [Required]
    public int Periods { get; set; } = 6;

    /// <summary>
    /// RSI threshold under which to perfrom entry buys.
    /// </summary>
    [Required]
    public decimal Oversold { get; set; } = 20M;

    /// <summary>
    /// RSI threshold above which never to perform top up buys.
    /// </summary>
    [Required]
    public decimal Overbought { get; set; } = 70M;

    /// <summary>
    /// Precision for RSI price prediction logic.
    /// </summary>
    [Required]
    public decimal Precision { get; set; } = 0.01M;
}

public class PortfolioAlgoOptionsRsiSell
{
    /// <summary>
    /// Periods for sell RSI calculation.
    /// </summary>
    [Required]
    public int Periods { get; set; } = 12;

    /// <summary>
    /// RSI threshold above which to attempt a sell.
    /// </summary>
    [Required]
    public decimal Overbought { get; set; } = 90M;
}