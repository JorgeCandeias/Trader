using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Portfolio;

public class PortfolioAlgoOptions
{
    [Required, Range(0, 1)]
    public decimal BalanceFractionPerBuy { get; set; } = 0.001M;

    [Required, Range(0, int.MaxValue)]
    public decimal MinRequiredRelativeValueForTopUpBuy { get; set; } = 1.05M;

    [Required, Range(0, int.MaxValue)]
    public decimal RelativeValueForPanicSell { get; set; } = 1.02M;

    [Required, Range(0, int.MaxValue)]
    public decimal BuyQuoteBalanceFraction { get; set; } = 0.001M;

    [Required, Range(0, int.MaxValue)]
    public decimal MinSellRate { get; set; } = 1.01M;

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
    [Required]
    public int Periods { get; set; } = 6;

    [Required]
    public decimal Oversold { get; set; } = 30M;
}

public class PortfolioAlgoOptionsRsiSell
{
    [Required]
    public int Periods { get; set; } = 12;

    [Required]
    public decimal Overbought { get; set; } = 95M;
}