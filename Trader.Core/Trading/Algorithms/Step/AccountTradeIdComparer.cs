using System;
using System.Collections.Generic;

namespace Trader.Core.Trading.Algorithms.Step
{
    internal class AccountTradeIdComparer : IComparer<AccountTrade>
    {
        private readonly bool _ascending;

        public AccountTradeIdComparer(bool ascending = true)
        {
            _ascending = ascending;
        }

        public int Compare(AccountTrade? x, AccountTrade? y)
        {
            if (x is null) throw new ArgumentNullException(nameof(x));
            if (y is null) throw new ArgumentNullException(nameof(y));

            return (_ascending ? 1 : -1) * x.Id < y.Id ? -1 : x.Id > y.Id ? 1 : 0;
        }
    }
}