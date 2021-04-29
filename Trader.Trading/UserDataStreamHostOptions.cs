using System;
using System.Collections.Generic;

namespace Trader.Trading
{
    public class UserDataStreamHostOptions
    {
        public ISet<string> Symbols { get; } = new HashSet<string>(StringComparer.Ordinal);
    }
}