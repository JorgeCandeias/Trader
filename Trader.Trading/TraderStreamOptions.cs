using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading
{
    public class TraderStreamOptions
    {
        public static string DefaultStreamProviderName { get; } = "Trader";

        [Required]
        public string StreamProviderName { get; set; } = DefaultStreamProviderName;

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan StreamRecoveryPeriod { get; set; } = TimeSpan.FromSeconds(1);
    }
}