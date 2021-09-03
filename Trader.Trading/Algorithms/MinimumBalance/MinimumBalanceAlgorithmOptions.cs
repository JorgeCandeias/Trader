using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms.MinimumBalance
{
    public class MinimumBalanceAlgorithmOptions
    {
        [Required]
        public string Asset { get; set; } = Empty;

        [Required]
        public decimal MinimumBalance { get; set; }
    }
}