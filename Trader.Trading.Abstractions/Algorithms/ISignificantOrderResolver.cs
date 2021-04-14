using System.Threading;
using System.Threading.Tasks;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    public interface ISignificantOrderResolver
    {
        Task<SignificantResult> ResolveAsync(string symbol, CancellationToken cancellationToken = default);
    }

    public record SignificantResult(SortedOrderSet Orders, Profit Profit, Statistics Statistics);

    public record Profit(decimal Today, decimal Yesterday, decimal ThisWeek, decimal PrevWeek, decimal ThisMonth, decimal ThisYear)
    {
        public static Profit Zero { get; } = new Profit(0, 0, 0, 0, 0, 0);
    }

    public record Statistics(decimal AvgPerHourToday);
}