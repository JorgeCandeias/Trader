using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoHostGrainOptions
    {
        [Required]
        public string Type { get; set; } = Empty;

        [Required]
        public bool Enabled { get; set; } = true;

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(1);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan TickDelay { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Run the algo on its own tick schedule as defined by <see cref="TickDelay"/>.
        /// </summary>
        [Required]
        public bool TickEnabled { get; set; } = false;

        /// <summary>
        /// Run the algo as part of the global batch schedule in an ordered fashion with other algos.
        /// </summary>
        [Required]
        public bool BatchEnabled { get; set; } = true;

        /// <summary>
        /// Describes the data dependencies for this algo instance.
        /// </summary>
        [Required]
        public AlgoHostGrainOptionsDependsOn DependsOn { get; } = new AlgoHostGrainOptionsDependsOn();
    }

    public class AlgoHostGrainOptionsDependsOn
    {
        public ISet<string> Tickers { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ISet<string> Balances { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IList<AlgoHostGrainOptionsKline> Klines { get; } = new List<AlgoHostGrainOptionsKline>();
    }

    public class AlgoHostGrainOptionsKline
    {
        [Required]
        public string Symbol { get; set; } = Empty;

        [Required]
        public KlineInterval Interval { get; set; }

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "9999.00:00:00.000")]
        public TimeSpan Window { get; set; }
    }
}