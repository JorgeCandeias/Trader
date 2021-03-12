using System;
using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Core.Trading.Algorithms.Step
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
        [Range(1.01, 2)]
        public decimal TargetMultiplier { get; set; } = 1.01m;

        [Required]
        [Range(0.001, 1)]
        public decimal TargetQuoteBalanceFractionPerBand { get; set; } = 0.01m;

        [Required]
        [Range(0, 100)]
        public decimal MinQuoteAssetQuantityPerOrder { get; set; } = 11m;

        [Required]
        [Range(1, 100)]
        public int MaxBands { get; set; } = 20;
    }
}