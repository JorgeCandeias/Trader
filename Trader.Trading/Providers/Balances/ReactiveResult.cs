using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Providers.Balances
{
    internal readonly struct ReactiveResult
    {
        public ReactiveResult(Guid version, Balance? value)
        {
            Version = version;
            Value = value;
        }

        public Guid Version { get; }
        public Balance? Value { get; }

        public void Deconstruct(out Guid version, out Balance? value)
        {
            version = Version;
            value = Value;
        }
    }
}