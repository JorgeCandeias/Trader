using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;

namespace Trader.Data.Sql
{
    internal record CancelOrderEntity(
        string Symbol,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        decimal Price,
        decimal OriginalQuantity,
        decimal ExecutedQuantity,
        decimal CummulativeQuoteQuantity,
        int Status,
        int TimeInForce,
        int Type,
        int Side);

    internal record OrderTableParameterEntity(
        string Symbol,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        decimal Price,
        decimal OriginalQuantity,
        decimal ExecutedQuantity,
        decimal CummulativeQuoteQuantity,
        decimal OriginalQuoteOrderQuantity,
        int Status,
        int TimeInForce,
        int Type,
        int Side,
        decimal StopPrice,
        decimal IcebergQuantity,
        DateTime Time,
        DateTime UpdateTime,
        bool IsWorking);

    internal static class OrderTableParameterEntityExtensions
    {
        /// <summary>
        /// Caches the sql metadata for the <see cref="ToSqlDataRecord"/> method.
        /// The order and types are important and must match 100% with the sql table valued parameter this record represents.
        /// </summary>
        private static readonly SqlMetaData[] _metadata = new[]
        {
            new SqlMetaData(nameof(OrderTableParameterEntity.Symbol), SqlDbType.NVarChar, 100),
            new SqlMetaData(nameof(OrderTableParameterEntity.OrderId), SqlDbType.BigInt),
            new SqlMetaData(nameof(OrderTableParameterEntity.OrderListId), SqlDbType.BigInt),
            new SqlMetaData(nameof(OrderTableParameterEntity.ClientOrderId), SqlDbType.NVarChar, 100),
            new SqlMetaData(nameof(OrderTableParameterEntity.Price), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(OrderTableParameterEntity.OriginalQuantity), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(OrderTableParameterEntity.ExecutedQuantity), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(OrderTableParameterEntity.CummulativeQuoteQuantity), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(OrderTableParameterEntity.OriginalQuoteOrderQuantity), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(OrderTableParameterEntity.Status), SqlDbType.Int),
            new SqlMetaData(nameof(OrderTableParameterEntity.TimeInForce), SqlDbType.Int),
            new SqlMetaData(nameof(OrderTableParameterEntity.Type), SqlDbType.Int),
            new SqlMetaData(nameof(OrderTableParameterEntity.Side), SqlDbType.Int),
            new SqlMetaData(nameof(OrderTableParameterEntity.StopPrice), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(OrderTableParameterEntity.IcebergQuantity), SqlDbType.Decimal, 18, 8),
            new SqlMetaData(nameof(OrderTableParameterEntity.Time), SqlDbType.DateTime2),
            new SqlMetaData(nameof(OrderTableParameterEntity.UpdateTime), SqlDbType.DateTime2),
            new SqlMetaData(nameof(OrderTableParameterEntity.IsWorking), SqlDbType.Bit)
        };

        /// <summary>
        /// Converts the specified entity to a <see cref="SqlDataRecord"/>.
        /// </summary>
        public static SqlDataRecord ToSqlDataRecord(this OrderTableParameterEntity entity)
        {
            _ = entity ?? throw new ArgumentNullException(nameof(entity));

            var record = new SqlDataRecord(_metadata);

            record.SetString(0, entity.Symbol);
            record.SetInt64(1, entity.OrderId);
            record.SetInt64(2, entity.OrderListId);
            record.SetString(3, entity.ClientOrderId);
            record.SetDecimal(4, entity.Price);
            record.SetDecimal(5, entity.OriginalQuantity);
            record.SetDecimal(6, entity.ExecutedQuantity);
            record.SetDecimal(7, entity.CummulativeQuoteQuantity);
            record.SetDecimal(8, entity.OriginalQuoteOrderQuantity);
            record.SetInt32(9, entity.Status);
            record.SetInt32(10, entity.TimeInForce);
            record.SetInt32(11, entity.Type);
            record.SetInt32(12, entity.Side);
            record.SetDecimal(13, entity.StopPrice);
            record.SetDecimal(14, entity.IcebergQuantity);
            record.SetDateTime(15, entity.Time);
            record.SetDateTime(16, entity.UpdateTime);
            record.SetBoolean(17, entity.IsWorking);

            return record;
        }

        public static IEnumerable<SqlDataRecord> AsSqlDataRecords(this IEnumerable<OrderTableParameterEntity> entities)
        {
            _ = entities ?? throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                yield return entity.ToSqlDataRecord();
            }
        }
    }

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