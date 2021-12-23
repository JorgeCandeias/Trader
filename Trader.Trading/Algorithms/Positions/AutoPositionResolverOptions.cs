using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

public class AutoPositionResolverOptions
{
    [Required]
    public TimeSpan ElapsedTimeWarningThreshold { get; set; } = TimeSpan.FromSeconds(1);
}