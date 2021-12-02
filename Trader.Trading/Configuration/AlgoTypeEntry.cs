namespace Outcompute.Trader.Trading.Configuration;

internal class AlgoTypeEntry : IAlgoTypeEntry
{
    public AlgoTypeEntry(string name, Type type)
    {
        Guard.IsNotNull(name, nameof(name));
        Guard.IsNotNull(type, nameof(type));

        Name = name;
        Type = type;
    }

    public string Name { get; }

    public Type Type { get; }
}