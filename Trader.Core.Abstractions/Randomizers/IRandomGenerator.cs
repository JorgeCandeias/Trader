using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Core.Randomizers;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
public interface IRandomGenerator
{
    int Next(int minValue, int maxValue);

    int Next(int maxValue);

    double NextDouble();
}