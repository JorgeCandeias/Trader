using Outcompute.Trader.Models;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Accumulator
{
    public class AccumulatorAlgoOptions
    {
        [Required, Range(1, int.MaxValue)]
        public int RsiPeriods { get; set; } = 6;

        [Required, Range(0, 100)]
        public int RsiOversold { get; set; } = 20;

        [Required, Range(0, 100)]
        public int RsiOverbought { get; set; } = 80;

        [Required]
        public KlineInterval RsiInterval { get; set; } = KlineInterval.Days1;
    }
}