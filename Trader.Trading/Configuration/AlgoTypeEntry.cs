using System;

namespace Outcompute.Trader.Trading.Configuration
{
    internal class AlgoTypeEntry : IAlgoTypeEntry
    {
        public AlgoTypeEntry(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public Type Type { get; }
    }
}