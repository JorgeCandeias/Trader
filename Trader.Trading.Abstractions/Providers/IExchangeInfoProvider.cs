using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers;

public interface IExchangeInfoProvider
{
    ExchangeInfo GetExchangeInfo();

    Symbol? TryGetSymbol(string name);
}