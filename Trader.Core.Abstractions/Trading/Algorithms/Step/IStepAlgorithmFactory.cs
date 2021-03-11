namespace Trader.Core.Trading.Algorithms.Step
{
    public interface IStepAlgorithmFactory
    {
        IStepAlgorithm Create(string name);
    }
}