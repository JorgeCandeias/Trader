using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Trading.Algorithms.Samples.Oscillator
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
        public decimal TakeProfitRate { get; set; } = 0.01M;

        [Required]
        public OscillatorAlgoOptionsStopLoss StopLoss { get; } = new();

        [Required]
        public OscillatorAlgoOptionsRsi Rsi { get; } = new();

        [Required]
        public OscillatorAlgoOptionsRmi Rmi { get; } = new();
    }

    public class OscillatorAlgoOptionsStopLoss
    {
        [Required]
        [Range(0, 1)]
        public decimal MinRate { get; set; } = 0.01M;

        [Required]
        [Range(0, 1)]
        [GreaterThan(nameof(MinRate))]
        public decimal MaxRate { get; set; } = 0.10M;

        [Required]
        [Range(0, 1)]
        public decimal LimitRate { get; set; } = 0.01M;
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
        [GreaterThan(nameof(Oversold))]
        public decimal Overbought { get; set; } = 70M;

        [Required]
        [Range(0, 1)]
        public decimal Precision { get; set; } = 0.01M;
    }

    public class OscillatorAlgoOptionsRmi
    {
        [Required]
        [Range(1, 1000)]
        public int MomentumPeriods { get; set; } = 3;

        [Required]
        [Range(1, 1000)]
        public int RmaPeriods { get; set; } = 14;

        [Required]
        [Range(0, 100)]
        public decimal Low { get; set; } = 30M;

        [Required]
        [Range(0, 100)]
        [GreaterThan(nameof(Low))]
        public decimal High { get; set; } = 70M;

        [Required]
        [Range(0, 1)]
        public decimal Precision { get; set; } = 0.01M;
    }
}