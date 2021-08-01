using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Trading.Algorithms.Change
{
    public class ChangeAlgorithmOptions
    {
        [Required]
        public string Symbol { get; set; } = Empty;

        [Required]
        public decimal BuySignalLowThreshold { get; set; } = 0.000m;

        [Required]
        public decimal BuySignalHighThreshold { get; set; } = 0.01m;

        [Required]
        public decimal TargetQuoteBalanceFraction { get; set; } = 0.001m;
    }
}