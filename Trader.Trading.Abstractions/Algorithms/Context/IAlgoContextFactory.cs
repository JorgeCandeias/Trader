namespace Outcompute.Trader.Trading.Algorithms.Context
{
    public interface IAlgoContextFactory
    {
        IAlgoContext Create(string name);
    }
}