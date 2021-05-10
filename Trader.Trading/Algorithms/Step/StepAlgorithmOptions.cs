using System;
using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Trading.Algorithms.Step
{
    public class StepAlgorithmOptions
    {
        [Required]
        public string Symbol { get; set; } = Empty;

        [Required]
        [Range(1, 99)]
        public int MaxBands { get; set; } = 99;

        [Required]
        [Range(0.01, 1)]
        public decimal PullbackRatio { get; set; } = 0.01m;

        [Required]
        [Range(0.001, 1)]
        public decimal TargetQuoteBalanceFractionPerBand { get; set; } = 0.01m;

        /// <summary>
        /// If <see cref="true"/> the algo will create the opening band automatically.
        /// If <see cref="false"/> the algo will keep working until the top band is closed and then not open again on its own.
        /// Use <see cref="false"/> to make the algo only keep working until the top band is closed.
        /// The algo will still continue to open lower bands for as long as the top band is open.
        /// </summary>
        [Required]
        public bool EnableOpening { get; set; } = true;

        /// <summary>
        /// If <see cref="true"/> the algo will keep creating lower bands below the top band as normal.
        /// If <see cref="false"/> the algo will no longer create any new lower bands.
        /// </summary>
        [Required]
        public bool EnableLowerBands { get; set; } = true;
    }
}