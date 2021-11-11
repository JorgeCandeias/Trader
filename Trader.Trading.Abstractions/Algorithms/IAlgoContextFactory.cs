namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoContextFactory
    {
        IAlgoContext Create(string name);
    }
}