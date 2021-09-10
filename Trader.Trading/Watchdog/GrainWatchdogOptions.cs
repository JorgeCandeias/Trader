using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Watchdog
{
    public class GrainWatchdogOptions
    {
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:01.000", "1.00:00:00.000")]
        public TimeSpan TickDelay { get; set; } = TimeSpan.FromSeconds(1);

        [Required]
        [Range(0.0, 1.0)]
        public double TickDelayRandomFactor { get; set; } = 0.2;
    }
}