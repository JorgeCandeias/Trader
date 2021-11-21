using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Portfolio;

public class PortfolioAlgoOptions
{
    [Required, Range(0, 1)]
    public decimal BalanceFractionPerBuy { get; set; } = 0.001M;

    [Required, Range(0, int.MaxValue)]
    public decimal MinChangeFromLastPositionPriceRequiredForTopUpBuy { get; set; } = 1.01M;

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
    /// <summary>
    /// Periods for RSI calculation.
    /// </summary>
    [Required]
    public int Periods { get; set; } = 6;

    /// <summary>
    /// RSI threshold at which to perfrom entry buys.
    /// </summary>
    [Required]
    public decimal Oversold { get; set; } = 30M;

    /// <summary>
    /// RSI threshold at which never to perform top up buys.
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
    /// RSI threshold at which to perform a sell.
    /// </summary>
    [Required]
    public decimal Overbought { get; set; } = 95M;
}