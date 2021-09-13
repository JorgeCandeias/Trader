using System;
using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoHostGrainOptions
    {
        [Required]
        public string Type { get; set; } = Empty;

        [Required]
        public bool Enabled { get; set; } = true;

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(1);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan TickDelay { get; set; } = TimeSpan.FromSeconds(10);

        [Required]
        public bool TickEnabled { get; set; } = true;
    }
}