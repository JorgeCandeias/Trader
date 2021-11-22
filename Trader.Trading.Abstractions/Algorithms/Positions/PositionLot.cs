using Orleans.Concurrency;
using System.Runtime.InteropServices;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

[Immutable]
[StructLayout(LayoutKind.Auto)]
public readonly record struct PositionLot(decimal Quantity, decimal AvgPrice);