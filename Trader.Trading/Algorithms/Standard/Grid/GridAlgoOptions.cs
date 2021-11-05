using System;
using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    public class GridAlgoOptions
    {
        [Required]
        [Range(1, 999)]
        public int MaxBands { get; set; } = 99;

        [Required]
        [Range(1, 99)]
        public int MaxActiveSellOrders { get; set; } = 1;

        [Required]
        [Range(0.01, 1)]
        public decimal PullbackRatio { get; set; } = 0.01m;

        [Required]
        [Range(0.001, 1)]
        public decimal BuyQuoteBalanceFraction { get; set; } = 0.01m;

        /// <summary>
        /// If <see cref="true"/> the algo will create the opening band automatically.
        /// If <see cref="false"/> the algo will keep working until the top band is closed and then not open again on its own.
        /// Use <see cref="false"/> to make the algo only keep working until the top band is closed.
        /// The algo will still continue to open lower bands for as long as the top band is open.
        /// </summary>
        [Required]
        public bool IsOpeningEnabled { get; set; } = true;

        /// <summary>
        /// If <see cref="true"/> the algo will keep creating lower bands below the top band as normal.
        /// If <see cref="false"/> the algo will no longer create any new lower bands.
        /// </summary>
        [Required]
        public bool IsLowerBandOpeningEnabled { get; set; } = true;

        [Required]
        public bool UseQuoteSavings { get; set; } = false;

        [Required]
        public bool UseQuoteSwapPool { get; set; } = false;

        [Required]
        public bool RedeemAssetSavings { get; set; } = false;

        [Required]
        public bool RedeemSwapPoolSavings { get; set; } = false;

        public decimal? MaxNotional { get; set; }
    }
}