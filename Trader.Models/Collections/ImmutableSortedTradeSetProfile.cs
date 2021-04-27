using AutoMapper;
using System.Collections.Generic;

namespace Trader.Models.Collections
{
    internal class ImmutableSortedTradeSetProfile : Profile
    {
        public ImmutableSortedTradeSetProfile()
        {
            CreateMap(typeof(IEnumerable<>), typeof(ImmutableSortedTradeSet)).ConvertUsing(typeof(ImmutableSortedTradeSetConverter<>));
        }
    }
}