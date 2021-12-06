﻿using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

public interface IAutoPositionResolver
{
    AutoPosition Resolve(Symbol symbol, ImmutableSortedOrderSet orders, TradeCollection trades, DateTime startTime);
}