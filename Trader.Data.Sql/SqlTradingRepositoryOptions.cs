using System;
using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Data.Sql
{
    public class SqlTradingRepositoryOptions
    {
        [Required]
        public string ConnectionString { get; set; } = Empty;

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:01.000", "0.00:01:00.000")]
        public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Helper for the implementation.
        /// </summary>
        internal int CommandTimeoutAsInteger => (int)Math.Ceiling(CommandTimeout.TotalSeconds);

        [Required]
        [Range(0, int.MaxValue)]
        public int RetryCount { get; set; } = 10;
    }
}