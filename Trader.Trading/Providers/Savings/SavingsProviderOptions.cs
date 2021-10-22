using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Providers.Savings
{
    public class SavingsProviderOptions
    {
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan SavingsCacheWindow { get; set; } = TimeSpan.FromMinutes(5);
    }
}