using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Providers.Savings;

public class SavingsProviderOptions
{
    /// <summary>
    /// How long between forced a refresh of the cached savings amounts.
    /// </summary>
    [Required]
    [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
    public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Cooldown betwen redemptions of each asset.
    /// </summary>
    public TimeSpan AssetCooldown { get; set; } = TimeSpan.FromMinutes(1);
}