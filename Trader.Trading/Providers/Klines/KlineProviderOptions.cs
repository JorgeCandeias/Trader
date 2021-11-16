using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Providers.Klines;

public class KlineProviderOptions
{
    [Required]
    [Range(0, int.MaxValue)]
    public int MaxCachedKlines { get; set; } = 1000;

    [Required]
    [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
    public TimeSpan CleanupPeriod { get; set; } = TimeSpan.FromMinutes(1);
}