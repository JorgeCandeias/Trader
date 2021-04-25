﻿using FastMember;

namespace Trader.Data.Sql
{
    internal static class MemberSetCache<T>
    {
        public static MemberSet MemberSet { get; } = TypeAccessorCache<T>.TypeAccessor.GetMembers();
    }
}