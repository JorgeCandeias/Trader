using AutoMapper;
using Trader.Models;

namespace Trader.Trading.Binance.Converters
{
    internal class OcoOrderStatusConverter : ITypeConverter<string, OcoOrderStatus>, ITypeConverter<OcoOrderStatus, string>
    {
        public string Convert(OcoOrderStatus source, string destination, ResolutionContext context)
        {
            return source switch
            {
                OcoOrderStatus.None => null!,

                OcoOrderStatus.Executing => "EXECUTING",
                OcoOrderStatus.AllDone => "ALL_DONE",
                OcoOrderStatus.Reject => "REJECT",

                _ => throw new AutoMapperMappingException($"Unknown {nameof(OcoOrderStatus)} '{source}'")
            };
        }

        public OcoOrderStatus Convert(string source, OcoOrderStatus destination, ResolutionContext context)
        {
            return source switch
            {
                null => OcoOrderStatus.None,

                "EXECUTING" => OcoOrderStatus.Executing,
                "ALL_DONE" => OcoOrderStatus.AllDone,
                "REJECT" => OcoOrderStatus.Reject,

                _ => throw new AutoMapperMappingException($"Unknown {nameof(OcoOrderStatus)} '{source}'")
            };
        }
    }
}