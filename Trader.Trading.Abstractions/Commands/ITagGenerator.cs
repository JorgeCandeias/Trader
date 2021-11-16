namespace Outcompute.Trader.Trading.Commands;

public interface ITagGenerator
{
    string Generate(string symbol, decimal price);
}