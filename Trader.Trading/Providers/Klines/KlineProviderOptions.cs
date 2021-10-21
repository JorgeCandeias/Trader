using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    public class KlineProviderOptions
    {
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan CleanupPeriod { get; set; } = TimeSpan.FromMinutes(1);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan SavePeriod { get; set; } = TimeSpan.FromSeconds(1);
    }
}