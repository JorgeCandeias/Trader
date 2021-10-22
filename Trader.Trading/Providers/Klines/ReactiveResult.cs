using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal readonly struct ReactiveResult
    {
        public ReactiveResult(Guid version, int serial, IReadOnlyList<Kline> items)
        {
            Version = version;
            Serial = serial;
            Items = items;
        }

        public Guid Version { get; }
        public int Serial { get; }
        public IReadOnlyList<Kline> Items { get; }
    }
}