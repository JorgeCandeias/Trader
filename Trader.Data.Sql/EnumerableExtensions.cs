using System.Data;
using Trader.Data.Sql;

namespace System.Collections.Generic
{
    internal static class EnumerableExtensions
    {
        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            var table = new DataTable(nameof(T));

            for (var i = 0; i < MemberSetCache<T>.MemberSet.Count; i++)
            {
                table.Columns.Add(MemberSetCache<T>.MemberSet[i].Name, MemberSetCache<T>.MemberSet[i].Type);
            }

            foreach (var item in items)
            {
                var row = table.NewRow();

                for (var i = 0; i < MemberSetCache<T>.MemberSet.Count; i++)
                {
                    row[i] = TypeAccessorCache<T>.TypeAccessor[item, MemberSetCache<T>.MemberSet[i].Name];
                }

                table.Rows.Add(row);
            }

            return table;
        }
    }
}