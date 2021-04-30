using System;
using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Trading.Algorithms.Step
{
    public class StepAlgorithmOptions
    {
        [Required]
        public string Symbol { get; set; } = Empty;

        [Required]
        public string Asset { get; set; } = Empty;

        [Required]
        public string Quote { get; set; } = Empty;

        [Required]
        [Range(1, 99)]
        public int MaxBands { get; set; } = 99;

        [Required]
        [Range(0.01, 1)]
        public decimal PullbackRatio { get; set; } = 0.01m;

        [Required]
        [Range(0.001, 1)]
        public decimal TargetQuoteBalanceFractionPerBand { get; set; } = 0.005m;

        [Required]
        [Range(1, 10)]
        public decimal ExtraAmountPerBandMultiplier { get; set; } = 1.00m;
    }
}