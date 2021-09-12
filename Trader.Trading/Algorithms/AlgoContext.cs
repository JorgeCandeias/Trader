namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoContext : IAlgoContext
    {
        public AlgoContext()
        {
            Name = AlgoFactoryContext.AlgoName;
        }

        public string Name { get; }
    }
}