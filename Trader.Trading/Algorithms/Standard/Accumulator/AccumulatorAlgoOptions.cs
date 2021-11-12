using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Accumulator
{
    public class AccumulatorAlgoOptions
    {
        [Required, Range(1, int.MaxValue)]
        public int RsiPeriods { get; set; } = 24;

        [Required, Range(0, 100)]
        public int RsiOversold { get; set; } = 20;

        [Required, Range(0, 100)]
        public int RsiOverbought { get; set; } = 80;

        [Required]
        public decimal NextBuyRate { get; set; } = 1.01M;

        [Required]
        public TimeSpan Cooldown { get; set; } = TimeSpan.FromSeconds(5);

        [Required]
        public decimal TakeProfitDropRate { get; set; } = 0.99M;
    }
}