using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.ValueAveraging
{
    public class ValueAveragingAlgoOptions
    {
        [Range(1.0, int.MaxValue)]
        public decimal StepRate { get; set; } = 1.01M;

        [Range(0.01, 0.99)]
        public decimal? PullbackRatio { get; set; }

        [Required]
        [Range(0.01, 1.00)]
        public decimal BuyOrderSafetyRatio { get; set; } = 1.0M;

        [Required]
        [Range(0.001, 1)]
        public decimal BuyQuoteBalanceFraction { get; set; } = 0.01m;

        [Required]
        [Range(1, 1000000)]
        public decimal MinSellProfitRate { get; set; } = 1.01m;

        [Required]
        [Range(1, 1000000)]
        public decimal TakeProfitRate { get; set; } = 1.10m;

        [Required]
        [Range(0, 1)]
        public decimal TrailingStopLossRate { get; set; } = 0.99M;

        [Required]
        public bool ElasticStopLossEnabled { get; set; } = true;

        public decimal? MaxNotional { get; set; }

        /// <summary>
        /// Whether to allow the algo to buy assets.
        /// </summary>
        [Required]
        public bool BuyingEnabled { get; set; } = true;

        [Required]
        public bool AccumulationEnabled { get; set; } = true;

        /// <summary>
        /// Whether to allow the algo to sell assets.
        /// </summary>
        [Required]
        public bool SellingEnabled { get; set; } = true;

        /// <summary>
        /// When enabled the algo will wait until a single sell of all assets can be placed.
        /// Use together with <see cref="MinSellProfitRate"/>=0 to wait for closing at cost value.
        /// </summary>
        [Required]
        public bool ClosingEnabled { get; set; } = false;

        [Required]
        public bool RedeemSavings { get; set; } = false;

        [Required]
        public bool RedeemSwapPool { get; set; } = false;

        [Required]
        public int SmaPeriodsA { get; set; } = 7;

        [Required]
        public int SmaPeriodsB { get; set; } = 25;

        [Required]
        public int SmaPeriodsC { get; set; } = 99;

        [Required]
        public int RsiPeriods { get; set; } = 6;

        [Required]
        public decimal RsiOverbought { get; set; } = 80m;

        [Required]
        public decimal RsiOversold { get; set; } = 20m;

        [Required]
        public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromDays(1);

        public int? MaxPositions { get; set; }

        public static ValueAveragingAlgoOptions Default { get; } = new ValueAveragingAlgoOptions();
    }
}