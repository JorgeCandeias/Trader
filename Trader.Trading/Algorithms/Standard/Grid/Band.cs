using System;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal sealed class Band
    {
        public Guid Id { get; } = Guid.NewGuid();

        public long OpenOrderId { get; set; }

        public decimal Quantity { get; set; }
        public decimal OpenPrice { get; set; }
        public BandStatus Status { get; set; }
        public long CloseOrderId { get; set; }
        public decimal ClosePrice { get; set; }
        public string CloseOrderClientId { get; set; } = string.Empty;
    }
}