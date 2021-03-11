using System;
using System.ComponentModel.DataAnnotations;

namespace Trader.Core.Trading.Binance
{
    public class BinanceOptions
    {
        [Required]
        public Uri BaseAddress { get; set; } = null!;

        [Required]
        public string ApiKey { get; set; } = null!;

        [Required]
        public string SecretKey { get; set; } = null!;

        [Required]
        [Range(0, 1)]
        public double UsageWarningRatio { get; set; } = 0.5;
    }
}