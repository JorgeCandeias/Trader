using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Samples.Oscillator;

public class OscillatorAlgoOptions
{
    [Required, Range(0, double.MaxValue)]
    public decimal EntryNotionalRate { get; set; } = 0.01M;

    [Required]
    public bool UseProfits { get; set; } = false;

    [Required]
    public bool LossEnabled { get; set; } = false;

    [Required]
    public ISet<string> ExcludeFromOpening { get; } = new HashSet<string>();
}