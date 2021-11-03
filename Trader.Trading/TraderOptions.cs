using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading
{
    public class TraderOptions
    {
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan BatchTickDelay { get; set; } = TimeSpan.FromSeconds(10);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan PingDelay { get; set; } = TimeSpan.FromSeconds(1);

        [Required]
        public bool BatchEnabled { get; set; } = true;

        public IDictionary<string, AlgoHostGrainOptions> Algos { get; } = new Dictionary<string, AlgoHostGrainOptions>();
    }
}