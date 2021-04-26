using AutoMapper;
using Trader.Models;

namespace Trader.Trading.Binance.Converters
{
    internal class OcoStatusConverter : ITypeConverter<string, OcoStatus>, ITypeConverter<OcoStatus, string>
    {
        public string Convert(OcoStatus source, string destination, ResolutionContext context)
        {
            return source switch
            {
                OcoStatus.None => null!,

                OcoStatus.Response => "RESPONSE",
                OcoStatus.ExecutionStarted => "EXEC_STARTED",
                OcoStatus.AllDone => "ALL_DONE",

                _ => throw new AutoMapperMappingException($"Unknown {nameof(OcoStatus)} '{source}'")
            };
        }

        public OcoStatus Convert(string source, OcoStatus destination, ResolutionContext context)
        {
            return source switch
            {
                null => OcoStatus.None,

                "RESPONSE" => OcoStatus.Response,
                "EXEC_STARTED" => OcoStatus.ExecutionStarted,
                "ALL_DONE" => OcoStatus.AllDone,

                _ => throw new AutoMapperMappingException($"Unknown {nameof(OcoStatus)} '{source}'")
            };
        }
    }
}