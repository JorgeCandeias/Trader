namespace Outcompute.Trader.Models;

public enum SymbolStatus
{
    None,
    PreTrading,
    Trading,
    PostTrading,
    EndOfDay,
    Halt,
    AuctionMatch,
    Break
}