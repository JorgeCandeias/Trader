using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoOptions
    {
        /// <summary>
        /// The type name of the algorithm to use.
        /// </summary>
        [Required]
        public string Type { get; set; } = Empty;

        /// <summary>
        /// The symbol for this algo. Only useful for algos that derive from <see cref="ISymbolAlgo"/>.
        /// </summary>
        public string Symbol { get; set; } = Empty;

        [Required]
        public bool Enabled { get; set; } = true;

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(1);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan TickDelay { get; set; } = TimeSpan.FromSeconds(10);

        [Required]
        [Range(typeof(TimeSpan), "0.00:00:00.001", "1.00:00:00.000")]
        public TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(10);

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
        public AlgoOptionsDependsOn DependsOn { get; } = new AlgoOptionsDependsOn();

        /// <summary>
        /// The relative run order in the batch.
        /// </summary>
        [Required]
        public int BatchOrder { get; set; } = 0;

        /// <summary>
        /// The start time for automatic position calculation.
        /// Only used when <see cref="Symbol"/> is also defined.
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; } = DateTime.MinValue;
    }

    public class AlgoOptionsDependsOn
    {
        public ISet<string> Tickers { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ISet<string> Balances { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IList<AlgoOptionsDependsOnKline> Klines { get; } = new List<AlgoOptionsDependsOnKline>();
    }

    public class AlgoOptionsDependsOnKline
    {
        [Required]
        public string Symbol { get; set; } = Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public KlineInterval Interval { get; set; } = KlineInterval.Days1;

        [Required]
        [Range(1, int.MaxValue)]
        public int Periods { get; set; } = 100;
    }

    public static class AlgoOptionsDependsOnKlineExtensions
    {
        public static void Add(this IList<AlgoOptionsDependsOnKline> list, string symbol, KlineInterval interval, int periods)
        {
            if (list is null) throw new ArgumentNullException(nameof(list));

            list.Add(new AlgoOptionsDependsOnKline
            {
                Symbol = symbol,
                Interval = interval,
                Periods = periods
            });
        }
    }
}