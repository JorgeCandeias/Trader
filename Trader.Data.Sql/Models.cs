using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;

namespace Trader.Data.Sql
{
    internal record TradeTableParameterEntity(
        string Symbol,
        long Id,
        long OrderId,
        long OrderListId,
        decimal Price,
        decimal Quantity,
        decimal QuoteQuantity,
        decimal Commission,
        string CommissionAsset,
        DateTime Time,
        bool IsBuyer,
        bool IsMaker,
        bool IsBestMatch);

    internal static class TradeTableParameterEntityExtensions
    {
        /// <summary>
        /// Caches the sql metadata for the <see cref="ToSqlDataRecord"/> method.
        /// The order and types are important and must match 100% with the sql table valued parameter this record represents.
        /// </summary>
        private static readonly SqlMetaData[] _metadata = new[]
        {
            new SqlMetaData(nameof(TradeTableParameterEntity.Symbol), SqlDbType.NVarChar, 100),
            new SqlMetaData(nameof(TradeTableParameterEntity.Id), SqlDbType.BigInt),
            new SqlMetaData(nameof(TradeTableParameterEntity.OrderId), SqlDbType.BigInt),
            new SqlMetaData(nameof(TradeTableParameterEntity.OrderListId), SqlDbType.BigInt),
            new SqlMetaData(nameof(TradeTableParameterEntity.Price), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(TradeTableParameterEntity.Quantity), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(TradeTableParameterEntity.QuoteQuantity), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(TradeTableParameterEntity.Commission), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(TradeTableParameterEntity.CommissionAsset), SqlDbType.NVarChar, 100),
            new SqlMetaData(nameof(TradeTableParameterEntity.Time), SqlDbType.DateTime2),
            new SqlMetaData(nameof(TradeTableParameterEntity.IsBuyer), SqlDbType.Bit),
            new SqlMetaData(nameof(TradeTableParameterEntity.IsMaker), SqlDbType.Bit),
            new SqlMetaData(nameof(TradeTableParameterEntity.IsBestMatch), SqlDbType.Bit)
        };

        /// <summary>
        /// Converts the specified entity to a <see cref="SqlDataRecord"/>.
        /// </summary>
        public static SqlDataRecord ToSqlDataRecord(this TradeTableParameterEntity entity)
        {
            _ = entity ?? throw new ArgumentNullException(nameof(entity));

            var record = new SqlDataRecord(_metadata);

            record.SetString(0, entity.Symbol);
            record.SetInt64(1, entity.Id);
            record.SetInt64(2, entity.OrderId);
            record.SetInt64(3, entity.OrderListId);
            record.SetDecimal(4, entity.Price);
            record.SetDecimal(5, entity.Quantity);
            record.SetDecimal(6, entity.QuoteQuantity);
            record.SetDecimal(7, entity.Commission);
            record.SetString(8, entity.CommissionAsset);
            record.SetDateTime(9, entity.Time);
            record.SetBoolean(10, entity.IsBuyer);
            record.SetBoolean(11, entity.IsMaker);
            record.SetBoolean(12, entity.IsBestMatch);

            return record;
        }

        public static IEnumerable<SqlDataRecord> AsSqlDataRecords(this IEnumerable<TradeTableParameterEntity> entities)
        {
            _ = entities ?? throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                yield return entity.ToSqlDataRecord();
            }
        }
    }
}