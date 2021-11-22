using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Oscillator
{
    public class OscillatorAlgoOptions
    {
        [Required, Range(double.Epsilon, double.MaxValue)]
        public decimal Notional { get; set; }

        [Required]
        [Range(0, 1)]
        public decimal FeeRate { get; set; } = 0.001M;

        [Required]
        public bool UseProfits { get; set; } = false;

        [Required]
        [Range(0, 1)]
        public decimal TakeProfitRate { get; set; } = 0.01M;

        [Required]
        [Range(0, 1)]
        public decimal StopLossRate { get; set; } = 0.01M;

        [Required]
        public OscillatorAlgoOptionsRsi Rsi { get; } = new();
    }

    public class OscillatorAlgoOptionsRsi
    {
        [Required]
        [Range(1, 1000)]
        public int Periods { get; set; } = 6;

        [Required]
        [Range(0, 100)]
        public decimal Oversold { get; set; } = 20M;

        [Required]
        [Range(0, 100)]
        public decimal Overbought { get; set; } = 70M;

        [Required]
        [Range(0, 1)]
        public decimal Precision { get; set; } = 0.01M;
    }
}