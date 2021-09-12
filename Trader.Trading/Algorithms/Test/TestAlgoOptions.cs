using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Test
{
    public class TestAlgoOptions
    {
        [Required]
        public string SomeValue { get; set; } = "Default";
    }
}