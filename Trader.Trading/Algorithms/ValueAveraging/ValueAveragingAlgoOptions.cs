using Outcompute.Trader.Models;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.ValueAveraging
{
    public class ValueAveragingAlgoOptions
    {
        [Required]
        [Range(0.01, 0.99)]
        public decimal PullbackRatio { get; set; } = 0.95m;

        [Required]
        [Range(0.01, 1.00)]
        public decimal BuyOrderSafetyRatio { get; set; } = 0.999m;

        [Required]
        [Range(0.001, 1)]
        public decimal TargetQuoteBalanceFractionPerBuy { get; set; } = 0.002m;

        [Required]
        [Range(1, 1000000)]
        public decimal MinSellProfitRate { get; set; } = 1.01m;

        [Required]
        [Range(1, 1000000)]
        public decimal TargetSellProfitRate { get; set; } = 1.10m;

        public decimal? MaxNotional { get; set; }

        [Required]
        public bool IsOpeningEnabled { get; set; } = true;

        [Required]
        public bool IsAveragingEnabled { get; set; } = true;

        [Required]
        public bool RedeemSavings { get; set; } = false;

        [Required]
        public KlineInterval KlineInterval { get; set; } = KlineInterval.Days1;

        [Required]
        public int SmaPeriodsA { get; set; } = 7;

        [Required]
        public int SmaPeriodsB { get; set; } = 25;

        [Required]
        public int SmaPeriodsC { get; set; } = 99;

        [Required]
        public int RsiPeriodsA { get; set; } = 6;

        [Required]
        public int RsiPeriodsB { get; set; } = 12;

        [Required]
        public int RsiPeriodsC { get; set; } = 24;

        [Required]
        public decimal RsiOverboughtA { get; set; } = 20m;

        [Required]
        public decimal RsiOversoldA { get; set; } = 50m;

        [Required]
        public decimal RsiOverboughtB { get; set; } = 20m;

        [Required]
        public decimal RsiOversoldB { get; set; } = 50m;

        [Required]
        public decimal RsiOverboughtC { get; set; } = 20m;

        [Required]
        public decimal RsiOversoldC { get; set; } = 50m;

        public static ValueAveragingAlgoOptions Default { get; } = new ValueAveragingAlgoOptions();
    }
}