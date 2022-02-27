using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Samples.Oscillator;

public class OscillatorAlgoOptions
{
    [Required, Range(double.Epsilon, double.MaxValue)]
    public decimal Notional { get; set; }

    [Required]
    public bool UseProfits { get; set; } = false;

    [Required]
    public ISet<string> ExcludeFromOpening { get; } = new HashSet<string>();
}