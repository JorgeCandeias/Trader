using Outcompute.Trader.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.PennyAccumulator
{
    public class PennyAccumulatorOptions
    {
        [Required]
        public string QuoteAsset { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int MaxAssetCount { get; set; } = 30;

        [Required]
        [Range(1, int.MaxValue)]
        public int RsiPeriods { get; set; } = 14;

        [Required]
        [Range(0, 100)]
        public int RsiOversold { get; set; } = 20;

        [Required]
        [Range(0, 100)]
        public int RsiOverbought { get; set; } = 80;

        [Required]
        public KlineInterval RsiInterval { get; set; } = KlineInterval.Days1;

        [Required]
        public TimeSpan CooloffPeriod { get; set; } = TimeSpan.FromDays(1);
    }

}