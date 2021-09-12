using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoConfigurationMappingOptions
    {
        [Required]
        public string ConfigurationRootKey { get; set; } = "Trader:Algos";

        [Required]
        public string ConfigurationOptionsSubKey { get; set; } = "Options";
    }
}