using System.Collections.Generic;

namespace Outcompute.Trader.Core.Serializers
{
    public interface IBase62NumberSerializer
    {
        string Serialize(long value);

        string Serialize(IEnumerable<long> items);

        long DeserializeOne(string value);

        IEnumerable<long> DeserializeMany(string values);
    }
}