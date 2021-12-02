namespace Outcompute.Trader.Trading.Configuration;

internal class AlgoEntry : IAlgoEntry
{
    public AlgoEntry(string name)
    {
        Guard.IsNotNull(name, nameof(name));

        Name = name;
    }

    public string Name { get; }
}