using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal readonly struct KlineResult
    {
        public KlineResult(Guid version, int serial, IReadOnlyList<Kline> klines)
        {
            Version = version;
            Serial = serial;
            Klines = klines;
        }

        public Guid Version { get; }
        public int Serial { get; }
        public IReadOnlyList<Kline> Klines { get; }
    }
}