using AutoMapper;
using System.Collections.Generic;

namespace Outcompute.Trader.Models.Collections
{
    internal class ImmutableSortedTradeSetProfile : Profile
    {
        public ImmutableSortedTradeSetProfile()
        {
            CreateMap(typeof(IEnumerable<>), typeof(ImmutableSortedTradeSet)).ConvertUsing(typeof(ImmutableSortedTradeSetConverter<>));
        }
    }
}