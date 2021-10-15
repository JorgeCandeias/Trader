using BenchmarkDotNet.Running;
using System.Reflection;

namespace Trader.Trading.Indicators.Benchmarks
{
    public static class Program
    {
        public static void Main()
        {
            BenchmarkSwitcher.FromAssembly(Assembly.GetEntryAssembly()).Run();
        }
    }
}