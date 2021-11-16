using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Providers.Swap;

public class SwapPoolOptions
{
    [Required]
    public bool AutoAddEnabled { get; set; }

    [Required]
    public bool AutoRedeemSavings { get; set; }

    public ISet<string> Assets { get; } = new HashSet<string>();

    public TimeSpan PoolCooldown { get; set; } = TimeSpan.FromMinutes(1);
}