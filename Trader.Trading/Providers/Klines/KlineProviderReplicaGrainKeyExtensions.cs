namespace Outcompute.Trader.Trading.Providers.Klines;

internal static class KlineProviderReplicaGrainKeyExtensions
{
    public static (string Symbol, KlineInterval Interval) GetPrimaryKeys(this IKlineProviderReplicaGrain grain)
    {
        if (grain is null) throw new ArgumentNullException(nameof(grain));

        var keys = grain.GetPrimaryKeyString().Split('|');
        var symbol = keys[0];
        var interval = Enum.Parse<KlineInterval>(keys[1], false);

        return (symbol, interval);
    }
}