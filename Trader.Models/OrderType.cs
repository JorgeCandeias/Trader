namespace Outcompute.Trader.Models;

public enum OrderType
{
    None = 0,
    Limit = 1,
    LimitMaker = 2,
    Market = 3,
    StopLoss = 4,
    StopLossLimit = 5,
    TakeProfit = 6,
    TakeProfitLimit = 7
}