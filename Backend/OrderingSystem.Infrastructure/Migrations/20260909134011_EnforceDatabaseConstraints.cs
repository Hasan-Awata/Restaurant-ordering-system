using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceDatabaseConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TableSessions_TableId_ClosedAt",
                table: "TableSessions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tax_Amount_NonNegative",
                table: "Taxes",
                sql: "\"Amount\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessions_TableId",
                table: "TableSessions",
                columns: new[] { "TableId", "ClosedAt" },
                unique: true,
                filter: "\"ClosedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tables_TableNumber_FloorNumber",
                table: "Tables",
                columns: new[] { "TableNumber", "FloorNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_TotalAmount_NonNegative",
                table: "Orders",
                sql: "\"TotalAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItem_Quantity_Positive",
                table: "OrderItems",
                sql: "\"Quantity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItem_UnitPrice_NonNegative",
                table: "OrderItems",
                sql: "\"UnitPrice\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tax_Amount_NonNegative",
                table: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_TableSessions_TableId",
                table: "TableSessions");

            migrationBuilder.DropIndex(
                name: "IX_Tables_TableNumber_FloorNumber",
                table: "Tables");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_TotalAmount_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItem_Quantity_Positive",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItem_UnitPrice_NonNegative",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessions_TableId_ClosedAt",
                table: "TableSessions",
                columns: new[] { "TableId", "ClosedAt" },
                filter: "\"ClosedAt\" IS NULL");
        }
    }
}
