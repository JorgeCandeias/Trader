using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoHostGrainOptions
    {
        [Required]
        public string Type { get; set; } = Empty;

        [Required]
        public bool Enabled { get; set; } = false;
    }
}