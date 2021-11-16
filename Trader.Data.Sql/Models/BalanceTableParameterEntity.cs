using Microsoft.Data.SqlClient.Server;
using System.Data;

namespace Outcompute.Trader.Data.Sql.Models;

internal record BalanceTableParameterEntity(
    string Asset,
    decimal Free,
    decimal Locked,
    DateTime UpdatedTime);

internal static class BalanceTableParameterExtensions
{
    /// <summary>
    /// Caches the sql metadata for the <see cref="ToSqlDataRecord"/> method.
    /// The order and types are important and must match 100% with the sql table valued parameter this record represents.
    /// </summary>
    private static readonly SqlMetaData[] _metadata = new[]
    {
            new SqlMetaData(nameof(BalanceTableParameterEntity.Asset), SqlDbType.NVarChar, 100),
            new SqlMetaData(nameof(BalanceTableParameterEntity.Free), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(BalanceTableParameterEntity.Locked), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(BalanceTableParameterEntity.UpdatedTime), SqlDbType.DateTime2)
        };

    /// <summary>
    /// Converts the specified entity to a <see cref="SqlDataRecord"/>.
    /// </summary>
    public static SqlDataRecord ToSqlDataRecord(this BalanceTableParameterEntity entity)
    {
        _ = entity ?? throw new ArgumentNullException(nameof(entity));

        var record = new SqlDataRecord(_metadata);

        record.SetString(0, entity.Asset);
        record.SetDecimal(1, entity.Free);
        record.SetDecimal(2, entity.Locked);
        record.SetDateTime(3, entity.UpdatedTime);

        return record;
    }

    public static IEnumerable<SqlDataRecord> AsSqlDataRecords(this IEnumerable<BalanceTableParameterEntity> entities)
    {
        _ = entities ?? throw new ArgumentNullException(nameof(entities));

        foreach (var entity in entities)
        {
            yield return entity.ToSqlDataRecord();
        }
    }
}