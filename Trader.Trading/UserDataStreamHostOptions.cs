using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Trader.Trading
{
    public class UserDataStreamHostOptions
    {
        public ISet<string> Symbols { get; } = new HashSet<string>(StringComparer.Ordinal);

        [Required]
        [Range(typeof(TimeSpan), "0.00:01:00.000", "0.00:59:00.000")]
        public TimeSpan PingPeriod { get; set; } = TimeSpan.FromMinutes(10);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.000", "0.00:01:00.000")]
        public TimeSpan StabilizationPeriod { get; set; } = TimeSpan.FromSeconds(5);
    }
}