using System;
using System.ComponentModel.DataAnnotations;

namespace Trader.Trading.Binance
{
    public class BinanceOptions
    {
        [Required]
        public Uri BaseApiAddress { get; set; } = null!;

        [Required]
        public Uri BaseWssAddress { get; set; } = null!;

        [Required]
        public string ApiKey { get; set; } = null!;

        [Required]
        public string SecretKey { get; set; } = null!;

        [Required]
        [Range(0, 1)]
        public double UsageWarningRatio { get; set; } = 0.5;

        [Required]
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}