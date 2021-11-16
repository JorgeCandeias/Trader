namespace Outcompute.Trader.Trading.Algorithms;

public interface IAlgoFactory
{
    IAlgo Create(string name);
}