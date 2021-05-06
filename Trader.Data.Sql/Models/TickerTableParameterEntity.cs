using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;

namespace Trader.Data.Sql.Models
{
    internal record TickerTableParameterEntity(
        int SymbolId,
        DateTime EventTime,
        decimal ClosePrice,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal AssetVolume,
        decimal QuoteVolume);

    internal static class TickerTableParameterEntityExtensions
    {
        /// <summary>
        /// Caches the sql metadata for the <see cref="ToSqlDataRecord"/> method.
        /// The order and types are important and must match 100% with the sql table valued parameter this record represents.
        /// </summary>
        private static readonly SqlMetaData[] _metadata = new[]
        {
            new SqlMetaData(nameof(TickerTableParameterEntity.SymbolId), SqlDbType.Int),
            new SqlMetaData(nameof(TickerTableParameterEntity.EventTime), SqlDbType.DateTime2),
            new SqlMetaData(nameof(TickerTableParameterEntity.ClosePrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(TickerTableParameterEntity.OpenPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(TickerTableParameterEntity.HighPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(TickerTableParameterEntity.LowPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(TickerTableParameterEntity.AssetVolume), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(TickerTableParameterEntity.QuoteVolume), SqlDbType.Decimal, 18, 8)
        };

        /// <summary>
        /// Converts the specified entity to a <see cref="SqlDataRecord"/>.
        /// </summary>
        public static SqlDataRecord ToSqlDataRecord(this TickerTableParameterEntity entity)
        {
            _ = entity ?? throw new ArgumentNullException(nameof(entity));

            var record = new SqlDataRecord(_metadata);

            record.SetInt32(0, entity.SymbolId);
            record.SetDateTime(1, entity.EventTime);
            record.SetDecimal(2, entity.ClosePrice);
            record.SetDecimal(3, entity.OpenPrice);
            record.SetDecimal(4, entity.HighPrice);
            record.SetDecimal(5, entity.LowPrice);
            record.SetDecimal(6, entity.AssetVolume);
            record.SetDecimal(7, entity.QuoteVolume);

            return record;
        }

        public static IEnumerable<SqlDataRecord> AsSqlDataRecords(this IEnumerable<TickerTableParameterEntity> entities)
        {
            _ = entities ?? throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                yield return entity.ToSqlDataRecord();
            }
        }
    }
}