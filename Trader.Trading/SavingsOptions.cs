using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading
{
    public class SavingsOptions
    {
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.000", "1.00:00:00.00")]
        public TimeSpan SavingsRedemptionDelay { get; set; } = TimeSpan.FromSeconds(10);
    }
}