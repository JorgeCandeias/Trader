using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Portfolio;

public class PortfolioAlgoOptions
{
    [Required, Range(0, 1)]
    public decimal BalanceFractionPerBuy { get; set; } = 0.001M;

    [Required, Range(0, int.MaxValue)]
    public decimal ReinforcementRate { get; set; } = 1.10M;

    [Required, Range(0, int.MaxValue)]
    public decimal MinRequiredRelativeValueForTopUpBuy { get; set; } = 1.10M;

    [Required, Range(0, int.MaxValue)]
    public decimal RelativeValueForPanicSell { get; set; } = 1.01M;

    [Required, Range(0, int.MaxValue)]
    public decimal BuyQuoteBalanceFraction { get; set; } = 0.001M;

    [Required, Range(0, int.MaxValue)]
    public decimal MinSellRate { get; set; } = 1.0M;

    [Range(0, int.MaxValue)]
    public decimal? MaxNotional { get; set; }

    [Required, Range(typeof(TimeSpan), "0.00:00:01.000", "365.00:00:00.000")]
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromDays(1);

    [Required]
    public bool UseSavings { get; set; }

    [Required]
    public bool UseSwapPools { get; set; }

    [Required]
    public PortfolioAlgoOptionsRsi Rsi { get; } = new PortfolioAlgoOptionsRsi();
}

public class PortfolioAlgoOptionsRsi
{
    [Required]
    public int Periods { get; set; } = 12;

    [Required]
    public decimal Oversold { get; set; } = 30M;
}