using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoConfigurationMappingOptions
    {
        [Required]
        public string TraderKey { get; set; } = "Trader";

        [Required]
        public string AlgosKey { get; set; } = "Trader:Algos";

        [Required]
        public string AlgoOptionsSubKey { get; set; } = "Options";
    }
}