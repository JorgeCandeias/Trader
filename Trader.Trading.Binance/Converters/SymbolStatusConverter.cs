using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class SymbolStatusConverter : ITypeConverter<string, SymbolStatus>
    {
        public SymbolStatus Convert(string source, SymbolStatus destination, ResolutionContext context)
        {
            return source switch
            {
                null => SymbolStatus.None,

                "PRE_TRADING" => SymbolStatus.PreTrading,
                "TRADING" => SymbolStatus.Trading,
                "POST_TRADING" => SymbolStatus.PostTrading,
                "END_OF_DAY" => SymbolStatus.EndOfDay,
                "HALT" => SymbolStatus.Halt,
                "AUCTION_MATCH" => SymbolStatus.AuctionMatch,
                "BREAK" => SymbolStatus.Break,

                _ => throw new AutoMapperMappingException($"Unknown {nameof(SymbolStatus)} {source}")
            };
        }
    }
}