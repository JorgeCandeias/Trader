namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoFactory
    {
        (IAlgo Algo, IAlgoContext Context) Create(string name);
    }
}