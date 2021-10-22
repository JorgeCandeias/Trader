using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Binance
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
        [Range(0, 1)]
        public double UsageBackoffRatio { get; set; } = 0.9;

        [Required]
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

        [Required]
        public TimeSpan DefaultBackoffPeriod { get; set; } = TimeSpan.FromMinutes(1);

        [Required]
        public string ApiKeyHeader { get; set; } = "X-MBX-APIKEY";

        [Required]
        public string UsedRequestWeightHeaderPrefix { get; set; } = "X-MBX-USED-WEIGHT-";

        [Required]
        public string UsedOrderCountHeaderPrefix { get; set; } = "X-MBX-ORDER-COUNT-";

        [Required]
        [Range(1, int.MaxValue)]
        public int MaxConcurrentApiRequests { get; set; } = 1;

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:01.000", "0.01:00:00.000")]
        public TimeSpan MarketDataStreamKeepAliveInterval { get; set; } = TimeSpan.FromMinutes(1);

        [Required]
        [Range(typeof(TimeSpan), "0.00:01:00.000", "1.00:00:00.000")]
        public TimeSpan MarketDataStreamResetPeriod { get; set; } = TimeSpan.FromHours(1);

        [Obsolete("Remove")]
        public ISet<string> UserDataStreamSymbols { get; } = new HashSet<string>(StringComparer.Ordinal);

        [Required]
        [Range(typeof(TimeSpan), "0.00:01:00.000", "0.00:59:00.000")]
        public TimeSpan UserDataStreamPingPeriod { get; set; } = TimeSpan.FromMinutes(1);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.000", "0.00:01:00.000")]
        public TimeSpan UserDataStreamStabilizationPeriod { get; set; } = TimeSpan.FromSeconds(5);

        [Required]
        [Range(typeof(TimeSpan), "0.00:01:00.000", "1.00:00:00.000")]
        public TimeSpan UserDataStreamResetPeriod { get; set; } = TimeSpan.FromHours(1);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan ReactivePollingDelay { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The maximum delay for ticker information to be broadcast across silos that host algos.
        /// Shorter delays will provide more timely data but will increase the load on the streaming grain and the silo that hosts it.
        /// Defaults to one second.
        /// </summary>
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan TickerBroadcastDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}