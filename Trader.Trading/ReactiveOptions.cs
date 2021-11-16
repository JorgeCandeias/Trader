using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading;

public class ReactiveOptions
{
    [Required]
    [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
    public TimeSpan ReactivePollingTimeout { get; set; } = TimeSpan.FromSeconds(20);

    [Required]
    [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
    public TimeSpan ReactiveTickDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    [Required]
    [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
    public TimeSpan ReactiveRecoveryDelay { get; set; } = TimeSpan.FromSeconds(1);
}