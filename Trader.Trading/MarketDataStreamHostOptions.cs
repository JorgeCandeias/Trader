using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Trader.Trading
{
    public class MarketDataStreamHostOptions
    {
        public ISet<string> Symbols { get; } = new HashSet<string>(StringComparer.Ordinal);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.000", "0.01:00:00.000")]
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(10);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.000", "0.00:01:00.000")]
        public TimeSpan StabilizationPeriod { get; set; } = TimeSpan.FromSeconds(5);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.000", "10.00:00:00.000")]
        public TimeSpan KlineHistorySpan { get; set; } = TimeSpan.FromHours(1);
    }
}