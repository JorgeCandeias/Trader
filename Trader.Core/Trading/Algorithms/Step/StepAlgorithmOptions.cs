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
        [Range(typeof(TimeSpan), "0.00:00:01.000", "0.01:00:00.000")]
        public TimeSpan Tick { get; set; } = TimeSpan.FromSeconds(1);

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