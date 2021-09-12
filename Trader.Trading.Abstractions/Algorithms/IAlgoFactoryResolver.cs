namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoFactoryResolver
    {
        IAlgoFactory Resolve(string typeName);
    }
}