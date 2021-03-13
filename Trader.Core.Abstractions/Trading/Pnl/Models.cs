namespace Trader.Core.Trading.ProfitCalculation
{
    public record Profit(decimal Today, decimal Yesterday, decimal ThisWeek, decimal ThisMonth, decimal ThisYear);
}