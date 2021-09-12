using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoConfigurationMappingOptions
    {
        [Required]
        public string RootKey { get; set; } = "Trader:Algos";

        [Required]
        public string EnabledSubKey { get; set; } = "Enabled";

        [Required]
        public string OptionsSubKey { get; set; } = "Options";
    }
}