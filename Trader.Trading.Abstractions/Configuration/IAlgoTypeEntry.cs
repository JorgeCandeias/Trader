using System;

namespace Outcompute.Trader.Trading.Configuration
{
    public interface IAlgoTypeEntry
    {
        string Name { get; }

        Type Type { get; }
    }
}