using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Providers.Exchange
{
    public class ExchangeInfoOptions
    {
        /// <summary>
        /// How often to refresh exchange information from the exchange.
        /// </summary>
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:01.000", "1.00:00:00.000")]
        public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// How often to propagate exchange information to local grain replicas.
        /// </summary>
        [Required]
        [Range(typeof(TimeSpan), "0.00:00:01.000", "1.00:00:00.000")]
        public TimeSpan PropagationPeriod { get; set; } = TimeSpan.FromSeconds(1);
    }
}