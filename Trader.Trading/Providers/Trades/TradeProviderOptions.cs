using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Providers.Trades
{
    public class TradeProviderOptions
    {
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan CleanupPeriod { get; set; } = TimeSpan.FromMinutes(1);
    }
}