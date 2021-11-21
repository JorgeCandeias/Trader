using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Portfolio;

public class PortfolioAlgoOptions
{
    [Required, Range(0, 1)]
    public decimal BalanceFractionPerBuy { get; set; } = 0.001M;

    [Required, Range(0, double.MaxValue)]
    public decimal MinChangeFromLastPositionPriceRequiredForTopUpBuy { get; set; } = 1.01M;

    [Required, Range(0, 1)]
    public decimal BuyQuoteBalanceFraction { get; set; } = 0.001M;

    /// <summary>
    /// Pad buy orders with this rate (before adjustments) to ensure that the resulting quantity is sellable.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal FeeRate { get; set; } = 0.001M;

    /// <summary>
    /// Whether selling logic is enabled.
    /// </summary>
    [Required]
    public bool SellingEnabled { get; set; } = true;

    /// <summary>
    /// The minimum profit rate at which to sell an asset as compared to its average cost.
    /// </summary>
    [Required, Range(0, double.MaxValue)]
    public decimal MinSellRate { get; set; } = 2M;

    /// <summary>
    /// Whether to enable stop loss logic.
    /// </summary>
    [Required]
    public bool StopLossEnabled { get; set; } = true;

    /// <summary>
    /// When the current price falls from the last position price by this rate, a market sell order for all sellable assets will be placed.
    /// </summary>
    [Required, Range(0, 1)]
    public decimal StopLossRateFromLastPosition { get; set; } = 0.90M;

    /// <summary>
    /// When stop loss is triggered, the sell order will only be placed if all assets can be sold at least at the this markup from their average cost.
    /// </summary>
    [Required, Range(0, int.MaxValue)]
    public decimal MinStopLossProfitRate { get; set; } = 1.01M;

    [Range(0, int.MaxValue)]
    public decimal? MaxNotional { get; set; }

    [Required, Range(typeof(TimeSpan), "0.00:00:01.000", "365.00:00:00.000")]
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromDays(1);

    [Required]
    public bool UseSavings { get; set; }

    [Required]
    public bool UseSwapPools { get; set; }

    [Required]
    public PortfolioAlgoOptionsRsis Rsi { get; } = new PortfolioAlgoOptionsRsis();

    /// <summary>
    /// Set of symbols to never issue sells against, even for stop loss.
    /// </summary>
    public ISet<string> NeverSellSymbols { get; } = new HashSet<string>();
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
    public decimal Oversold { get; set; } = 30M;

    /// <summary>
    /// RSI threshold above which never to perform top up buys.
    /// </summary>
    [Required]
    public decimal Overbought { get; set; } = 70M;
}

public class PortfolioAlgoOptionsRsiSell
{
    /// <summary>
    /// Periods for sell RSI calculation.
    /// </summary>
    [Required]
    public int Periods { get; set; } = 12;

    /// <summary>
    /// RSI threshold above which to perform a sell.
    /// </summary>
    [Required]
    public decimal Overbought { get; set; } = 95M;
}