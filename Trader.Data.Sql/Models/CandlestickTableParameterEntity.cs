using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;

namespace Trader.Data.Sql.Models
{
    internal record CandlestickTableParameterEntity(
        int SymbolId,
        int Interval,
        DateTime OpenTime,
        DateTime CloseTime,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal ClosePrice,
        decimal Volume,
        decimal QuoteAssetVolume,
        int TradeCount,
        decimal TakerBuyBaseAssetVolume,
        decimal TakerBuyQuoteAssetVolume);

    internal static class CandlestickTableParameterEntityExtensions
    {
        /// <summary>
        /// Caches the sql metadata for the <see cref="ToSqlDataRecord"/> method.
        /// The order and types are important and must match 100% with the sql table valued parameter this record represents.
        /// </summary>
        private static readonly SqlMetaData[] _metadata = new[]
        {
            new SqlMetaData(nameof(CandlestickTableParameterEntity.SymbolId), SqlDbType.Int),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.Interval), SqlDbType.Int),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.OpenTime), SqlDbType.DateTime2),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.CloseTime), SqlDbType.DateTime2),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.OpenPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.HighPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.LowPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.ClosePrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.Volume), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.QuoteAssetVolume), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.TradeCount), SqlDbType.Int),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.TakerBuyBaseAssetVolume), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(CandlestickTableParameterEntity.TakerBuyQuoteAssetVolume), SqlDbType.Decimal, 18, 8)
        };

        /// <summary>
        /// Converts the specified entity to a <see cref="SqlDataRecord"/>.
        /// </summary>
        public static SqlDataRecord ToSqlDataRecord(this CandlestickTableParameterEntity entity)
        {
            _ = entity ?? throw new ArgumentNullException(nameof(entity));

            var record = new SqlDataRecord(_metadata);

            record.SetInt32(0, entity.SymbolId);
            record.SetInt32(1, entity.Interval);
            record.SetDateTime(2, entity.OpenTime);
            record.SetDateTime(3, entity.CloseTime);
            record.SetDecimal(4, entity.OpenPrice);
            record.SetDecimal(5, entity.HighPrice);
            record.SetDecimal(6, entity.LowPrice);
            record.SetDecimal(7, entity.ClosePrice);
            record.SetDecimal(8, entity.Volume);
            record.SetDecimal(9, entity.QuoteAssetVolume);
            record.SetInt32(10, entity.TradeCount);
            record.SetDecimal(11, entity.TakerBuyBaseAssetVolume);
            record.SetDecimal(12, entity.TakerBuyQuoteAssetVolume);

            return record;
        }

        public static IEnumerable<SqlDataRecord> AsSqlDataRecords(this IEnumerable<CandlestickTableParameterEntity> entities)
        {
            _ = entities ?? throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                yield return entity.ToSqlDataRecord();
            }
        }
    }
}