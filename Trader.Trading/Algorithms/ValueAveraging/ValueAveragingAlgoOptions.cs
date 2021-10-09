using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms.ValueAveraging
{
    public class ValueAveragingAlgoOptions
    {
        [Required]
        public string Symbol { get; set; } = Empty;

        [Required]
        [Range(0.01, 0.99)]
        public decimal PullbackRatio { get; set; } = 0.90m;

        [Required]
        [Range(0.001, 1)]
        public decimal TargetQuoteBalanceFractionPerBuy { get; set; } = 0.001m;

        [Required]
        [Range(1, 2)]
        public decimal ProfitMultipler { get; set; } = 1.1m;

        public decimal? MaxNotional { get; set; }

        [Required]
        public bool IsOpeningEnabled { get; set; } = true;

        [Required]
        public bool IsAveragingEnabled { get; set; } = true;

        [Required]
        public bool UseSavings { get; set; } = false;
    }
}