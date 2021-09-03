using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class ContingencyTypeConverter : ITypeConverter<string, ContingencyType>, ITypeConverter<ContingencyType, string>
    {
        public string Convert(ContingencyType source, string destination, ResolutionContext context)
        {
            return source switch
            {
                ContingencyType.None => null!,

                ContingencyType.Oco => "OCO",

                _ => throw new AutoMapperMappingException($"Unknown {nameof(ContingencyType)} '{source}'")
            };
        }

        public ContingencyType Convert(string source, ContingencyType destination, ResolutionContext context)
        {
            return source switch
            {
                null => ContingencyType.None,

                "OCO" => ContingencyType.Oco,

                _ => throw new AutoMapperMappingException($"Unknown {nameof(ContingencyType)} '{source}'")
            };
        }
    }
}