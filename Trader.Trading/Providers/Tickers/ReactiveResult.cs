using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Providers.Tickers
{
    internal readonly struct ReactiveResult
    {
        public ReactiveResult(Guid version, MiniTicker? item)
        {
            Version = version;
            Item = item;
        }

        public Guid Version { get; }
        public MiniTicker? Item { get; }

        public void Deconstruct(out Guid version, out MiniTicker? item)
        {
            version = Version;
            item = Item;
        }
    }
}