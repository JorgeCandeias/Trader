using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Trading.Algorithms.Accumulator
{
    public class AccumulatorAlgorithmOptions
    {
        [Required]
        public string Symbol { get; set; } = Empty;

        [Required]
        public string Asset { get; set; } = Empty;

        [Required]
        public string Quote { get; set; } = Empty;

        [Required]
        [Range(0.01, 0.99)]
        public decimal PullbackRatio { get; set; } = 0.90m;

        [Required]
        [Range(0.001, 1)]
        public decimal TargetQuoteBalanceFractionPerBuy { get; set; } = 0.01m;

        [Required]
        [Range(0, (double)decimal.MaxValue)]
        public decimal MinQuoteAssetQuantityPerOrder { get; set; } = 11m;
    }
}