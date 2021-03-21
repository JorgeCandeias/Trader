using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Data.Migrations
{
    public partial class Create : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    OrderListId = table.Column<long>(type: "INTEGER", nullable: false),
                    ClientOrderId = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExecutedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    CummulativeQuoteQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeInForce = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Side = table.Column<int>(type: "INTEGER", nullable: false),
                    StopPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    IcebergQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsWorking = table.Column<bool>(type: "INTEGER", nullable: false),
                    OriginalQuoteOrderQuantity = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Symbol_OrderId",
                table: "Orders",
                columns: new[] { "Symbol", "OrderId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
