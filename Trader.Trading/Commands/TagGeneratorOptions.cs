using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Commands;

public class TagGeneratorOptions
{
    [Required]
    [Range(1, int.MaxValue)]
    public int MaxTagLength { get; set; } = 30;
}