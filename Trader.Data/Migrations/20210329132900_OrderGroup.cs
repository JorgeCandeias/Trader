using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Data.Migrations
{
    public partial class OrderGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderGroupDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupId = table.Column<long>(type: "INTEGER", nullable: false),
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderGroupDetails", x => x.Id);
                    table.UniqueConstraint("AK_OrderGroupDetails_GroupId_OrderId", x => new { x.GroupId, x.OrderId });
                    table.ForeignKey(
                        name: "FK_OrderGroupDetails_OrderGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "OrderGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderGroupDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroupDetails_OrderId",
                table: "OrderGroupDetails",
                column: "OrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderGroupDetails");

            migrationBuilder.DropTable(
                name: "OrderGroups");
        }
    }
}
