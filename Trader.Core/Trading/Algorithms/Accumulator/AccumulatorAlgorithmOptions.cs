using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Core.Trading.Algorithms.Accumulator
{
    public class AccumulatorAlgorithmOptions
    {
        [Required]
        public string Symbol { get; set; } = Empty;

        [Required]
        public string Asset { get; set; } = Empty;

        [Required]
        public string Quote { get; set; } = Empty;
    }
}