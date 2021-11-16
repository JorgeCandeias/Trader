namespace Outcompute.Trader.Trading.Configuration;

internal class AlgoEntry : IAlgoEntry
{
    public AlgoEntry(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}