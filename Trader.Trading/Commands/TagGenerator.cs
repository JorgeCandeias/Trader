using Microsoft.Extensions.Options;
using System.Globalization;

namespace Outcompute.Trader.Trading.Commands
{
    internal class TagGenerator : ITagGenerator
    {
        private readonly TagGeneratorOptions _options;

        public TagGenerator(IOptions<TagGeneratorOptions> options)
        {
            _options = options.Value;
        }

        public string Generate(string symbol, decimal price)
        {
            Span<char> span = stackalloc char[_options.MaxTagLength + 1];
            var count = 0;

            // write the symbol to the tag
            if (!symbol.TryCopyTo(span))
            {
                throw new InvalidOperationException();
            }
            count += symbol.Length;

            // write the price to the tag
            if (!price.TryFormat(span[count..], out var written, "F8", CultureInfo.InvariantCulture))
            {
                throw new InvalidOperationException();
            }
            count += written;

            // cleanup the tag from disallowed chars
            Span<char> clean = stackalloc char[_options.MaxTagLength];
            var j = 0;
            for (var i = 0; i < count; i++)
            {
                if (span[i] != '.')
                {
                    clean[j] = span[i];
                    j++;
                }
            }

            return clean[..j].ToString();
        }
    }
}