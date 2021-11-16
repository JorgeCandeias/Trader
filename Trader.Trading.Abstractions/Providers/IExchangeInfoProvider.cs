using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers;

public interface IExchangeInfoProvider
{
    ValueTask<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default);

    ValueTask<Symbol?> TryGetSymbolAsync(string name, CancellationToken cancellationToken = default);
}