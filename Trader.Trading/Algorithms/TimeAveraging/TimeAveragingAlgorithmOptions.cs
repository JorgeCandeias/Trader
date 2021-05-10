using System;
using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Trading.Algorithms.TimeAveraging
{
    public class TimeAveragingAlgorithmOptions
    {
        [Required]
        public string Symbol { get; set; } = Empty;

        [Required]
        [Range(typeof(TimeSpan), "0.00:01:00.000", "1.00:00:00.000")]
        public TimeSpan Period { get; set; } = TimeSpan.FromHours(1);

        [Required]
        [Range(typeof(decimal), "0.001", "1")]
        public decimal QuoteFractionPerBuy { get; set; } = 0.01m;
    }
}