using Outcompute.Trader.Models;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms;

public class AlgoOptions
{
    /// <summary>
    /// The type name of the algorithm to use.
    /// </summary>
    [Required]
    public string Type { get; set; } = Empty;

    /// <summary>
    /// The default symbol for this algo.
    /// </summary>
    public string Symbol { get; set; } = Empty;

    /// <summary>
    /// The default kline interval for this algo.
    /// </summary>
    public KlineInterval KlineInterval { get; set; } = KlineInterval.None;

    /// <summary>
    /// The default kline periods for this algo.
    /// </summary>
    public int KlinePeriods { get; set; }

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
    /// <summary>
    /// Symbol for which to load klines.
    /// </summary>
    public string? Symbol { get; set; }

    /// <summary>
    /// Interval for which to load klines.
    /// </summary>
    public KlineInterval Interval { get; set; } = KlineInterval.None;

    /// <summary>
    /// The rolling number of klines to load.
    /// </summary>
    public int Periods { get; set; }
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