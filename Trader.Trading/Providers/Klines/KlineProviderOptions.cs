using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    public class KlineProviderOptions
    {
        /// <summary>
        /// How often to save klines to the repository.
        /// </summary>
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan SavePeriod { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// How often to cleanup old klines from the memory cache.
        /// </summary>
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan CleanupPeriod { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// How many recent klines to keep cached in memory per (symbol/interval) pair.
        /// Algos will only have access to these many recent klines from the kline provider.
        /// </summary>
        [Required]
        [Range(0, int.MaxValue)]
        public int MaxCachedKlines { get; set; } = 1000;

        /// <summary>
        /// How often to propagate klines to replicas.
        /// </summary>
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan PropagationPeriod { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// How long to wait between retries on publishing failure.
        /// </summary>
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan PublishRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}