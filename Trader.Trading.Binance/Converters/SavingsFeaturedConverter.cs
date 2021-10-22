using AutoMapper;
using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class SavingsFeaturedConverter : ITypeConverter<SavingsFeatured, string>, ITypeConverter<string, SavingsFeatured>
    {
        public string Convert(SavingsFeatured source, string destination, ResolutionContext context)
        {
            return source switch
            {
                SavingsFeatured.None => "",

                SavingsFeatured.All => "ALL",
                SavingsFeatured.True => "TRUE",

                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }

        public SavingsFeatured Convert(string source, SavingsFeatured destination, ResolutionContext context)
        {
            return source switch
            {
                null => SavingsFeatured.None,
                "" => SavingsFeatured.None,

                "ALL" => SavingsFeatured.All,
                "TRUE" => SavingsFeatured.True,

                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }
    }
}