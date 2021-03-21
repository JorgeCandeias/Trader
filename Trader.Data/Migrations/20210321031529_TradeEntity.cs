using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Data.Migrations
{
    public partial class TradeEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeEntity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    OrderListId = table.Column<long>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    QuoteQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Commission = table.Column<decimal>(type: "TEXT", nullable: false),
                    CommissionAsset = table.Column<string>(type: "TEXT", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsBuyer = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMaker = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBestMatch = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeEntity", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeEntity_Symbol_Id",
                table: "TradeEntity",
                columns: new[] { "Symbol", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TradeEntity_Symbol_OrderId",
                table: "TradeEntity",
                columns: new[] { "Symbol", "OrderId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeEntity");
        }
    }
}
