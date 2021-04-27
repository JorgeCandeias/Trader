using AutoMapper;
using System.Collections.Generic;

namespace Trader.Models.Collections
{
    internal class ImmutableSortedOrderSetProfile : Profile
    {
        public ImmutableSortedOrderSetProfile()
        {
            CreateMap(typeof(IEnumerable<>), typeof(ImmutableSortedOrderSet)).ConvertUsing(typeof(ImmutableSortedOrderSetConverter<>));
        }
    }
}