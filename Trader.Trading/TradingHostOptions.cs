using System;
using System.ComponentModel.DataAnnotations;

namespace Trader.Trading
{
    public class TradingHostOptions
    {
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:01.000", "1.00:00:00.000")]
        public TimeSpan TickPeriod { get; set; } = TimeSpan.FromSeconds(10);

        [Required]
        [Range(typeof(TimeSpan), "0.00:01:00.000", "0.01:00:00.000")]
        public TimeSpan TickTimeout { get; set; } = TimeSpan.FromMinutes(1);

        [Required]
        [Range(typeof(TimeSpan), "0.00:01:00.000", "0.01:00:00.000")]
        public TimeSpan TickTimeoutWithDebugger { get; set; } = TimeSpan.FromHours(1);
    }
}