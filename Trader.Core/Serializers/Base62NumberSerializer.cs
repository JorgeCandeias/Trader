using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Outcompute.Trader.Core.Serializers
{
    internal class Base62NumberSerializer : IBase62NumberSerializer
    {
        public string Serialize(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

            using var buffer = MemoryPool<char>.Shared.Rent(11);
            var span = buffer.Memory.Span;
            var index = span.Length;

            // convert to the serialized base
            while (value > 0)
            {
                value = Math.DivRem(value, _map.Length, out var remainder);

                span[--index] = _map[(int)remainder];
            }

            // return the valid chars
            return span[index..].ToString();
        }

        public string Serialize(IEnumerable<long> items)
        {
            var builder = new StringBuilder();
            var count = 0;
            foreach (var item in items)
            {
                if (count++ > 0)
                {
                    builder.Append('_');
                }

                builder.Append(Serialize(item));
            }
            return builder.ToString();
        }

        public long DeserializeOne(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var result = 0L;

            for ((int i, long weight) = (value.Length - 1, 1); i >= 0; i--, weight *= _map.Length)
            {
                var glyph = value[i];
                if (_reverse.TryGetValue(glyph, out var amount))
                {
                    result += amount * weight;
                }
                else
                {
                    throw new FormatException($"Value '{value}' could not be deserialized.");
                }
            }

            return result;
        }

        public IEnumerable<long> DeserializeMany(string values)
        {
            if (values is null) throw new ArgumentNullException(nameof(values));

            var splits = values.Split('_');

            var result = new long[splits.Length];
            for (var i = 0; i < result.Length; ++i)
            {
                result[i] = DeserializeOne(splits[i]);
            }
            return result;
        }

        private static readonly char[] _map = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        private static readonly Dictionary<char, int> _reverse = _map.Select((item, index) => (item, index)).ToDictionary(x => x.item, x => x.index);
    }
}