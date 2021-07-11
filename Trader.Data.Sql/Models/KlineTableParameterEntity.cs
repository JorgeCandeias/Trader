using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;

namespace Trader.Data.Sql.Models
{
    internal record KlineTableParameterEntity(
        int SymbolId,
        int Interval,
        DateTime OpenTime,
        DateTime CloseTime,
        DateTime EventTime,
        long FirstTradeId,
        long LastTradeId,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal ClosePrice,
        decimal Volume,
        decimal QuoteAssetVolume,
        int TradeCount,
        bool IsClosed,
        decimal TakerBuyBaseAssetVolume,
        decimal TakerBuyQuoteAssetVolume);

    internal static class KlineTableParameterEntityExtensions
    {
        /// <summary>
        /// Caches the sql metadata for the <see cref="ToSqlDataRecord"/> method.
        /// The order and types are important and must match 100% with the sql table valued parameter this record represents.
        /// </summary>
        private static readonly SqlMetaData[] _metadata = new[]
        {
            new SqlMetaData(nameof(KlineTableParameterEntity.SymbolId), SqlDbType.Int),
            new SqlMetaData(nameof(KlineTableParameterEntity.Interval), SqlDbType.Int),
            new SqlMetaData(nameof(KlineTableParameterEntity.OpenTime), SqlDbType.DateTime2),
            new SqlMetaData(nameof(KlineTableParameterEntity.CloseTime), SqlDbType.DateTime2),
            new SqlMetaData(nameof(KlineTableParameterEntity.EventTime), SqlDbType.DateTime2),
            new SqlMetaData(nameof(KlineTableParameterEntity.FirstTradeId), SqlDbType.BigInt),
            new SqlMetaData(nameof(KlineTableParameterEntity.LastTradeId), SqlDbType.BigInt),
            new SqlMetaData(nameof(KlineTableParameterEntity.OpenPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(KlineTableParameterEntity.HighPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(KlineTableParameterEntity.LowPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(KlineTableParameterEntity.ClosePrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(KlineTableParameterEntity.Volume), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(KlineTableParameterEntity.QuoteAssetVolume), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(KlineTableParameterEntity.TradeCount), SqlDbType.Int),
            new SqlMetaData(nameof(KlineTableParameterEntity.IsClosed), SqlDbType.Bit),
            new SqlMetaData(nameof(KlineTableParameterEntity.TakerBuyBaseAssetVolume), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(KlineTableParameterEntity.TakerBuyQuoteAssetVolume), SqlDbType.Decimal, 18, 8)
        };

        /// <summary>
        /// Converts the specified entity to a <see cref="SqlDataRecord"/>.
        /// </summary>
        public static SqlDataRecord ToSqlDataRecord(this KlineTableParameterEntity entity)
        {
            _ = entity ?? throw new ArgumentNullException(nameof(entity));

            var record = new SqlDataRecord(_metadata);

            record.SetInt32(0, entity.SymbolId);
            record.SetInt32(1, entity.Interval);
            record.SetDateTime(2, entity.OpenTime);
            record.SetDateTime(3, entity.CloseTime);
            record.SetDateTime(4, entity.EventTime);
            record.SetInt64(5, entity.FirstTradeId);
            record.SetInt64(6, entity.LastTradeId);
            record.SetDecimal(7, entity.OpenPrice);
            record.SetDecimal(8, entity.HighPrice);
            record.SetDecimal(9, entity.LowPrice);
            record.SetDecimal(10, entity.ClosePrice);
            record.SetDecimal(11, entity.Volume);
            record.SetDecimal(12, entity.QuoteAssetVolume);
            record.SetInt32(13, entity.TradeCount);
            record.SetBoolean(14, entity.IsClosed);
            record.SetDecimal(15, entity.TakerBuyBaseAssetVolume);
            record.SetDecimal(16, entity.TakerBuyQuoteAssetVolume);

            return record;
        }

        public static IEnumerable<SqlDataRecord> AsSqlDataRecords(this IEnumerable<KlineTableParameterEntity> entities)
        {
            _ = entities ?? throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                yield return entity.ToSqlDataRecord();
            }
        }
    }
}